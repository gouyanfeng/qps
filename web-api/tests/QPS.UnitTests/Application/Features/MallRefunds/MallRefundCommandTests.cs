using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Moq;
using QPS.Mall.Application.Features.MallRefunds;
using QPS.Mall.Application.Features.MallReturns;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Persistence.Database;
using QPS.Shared.Exceptions;
using QPS.UnitTests.Common;
using Xunit;

namespace QPS.UnitTests.Application.Features.MallRefunds;

/// <summary>
/// 整单退款规则：退款金额 = 订单实付金额（含运费），拒绝部分退款、重复退款，
/// 退款成功后回补库存、退回券并同步订单与支付状态。
/// </summary>
public class MallRefundCommandTests
{
    [Fact]
    public async Task CreateRefund_WholeOrder_RefundsPaidAmountIncludingFreight()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var seeded = await SeedPaidOrderAsync(db, goodsAmount: 120m, freightAmount: 10m);

        var refund = await new CreateMallRefundHandler(db).Handle(
            new CreateMallRefundCommand
            {
                OrderId = seeded.Order.Id,
                MemberId = seeded.Order.MemberId,
                Request = new CreateMallRefundRequest { Reason = "不想要了", Amount = 130m }
            },
            CancellationToken.None);

        // 实付 120 + 运费 10 = 130，运费随实付一起退
        Assert.Equal(130m, refund.Amount);
        Assert.Equal(MallRefundStatus.Pending, refund.Status);

        db.ChangeTracker.Clear();
        var order = await db.MallOrders.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Order.Id);
        Assert.Equal(MallOrderStatus.Refunding, order.Status);
        Assert.Equal(130m, order.PaidAmount);

        var refundEntity = await db.MallRefunds.AsNoTracking()
            .FirstAsync(item => item.Id == refund.Id);
        Assert.Equal(130m, refundEntity.Amount);
    }

    [Fact]
    public async Task CreateRefund_RejectsMismatchedAmount()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var seeded = await SeedPaidOrderAsync(db, goodsAmount: 120m, freightAmount: 10m);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            new CreateMallRefundHandler(db).Handle(
                new CreateMallRefundCommand
                {
                    OrderId = seeded.Order.Id,
                    MemberId = seeded.Order.MemberId,
                    Request = new CreateMallRefundRequest { Reason = "部分退款", Amount = 100m }
                },
                CancellationToken.None));

        Assert.Contains("仅支持整单退款", exception.Message);

        db.ChangeTracker.Clear();
        Assert.Equal(
            MallOrderStatus.Paid,
            (await db.MallOrders.AsNoTracking().FirstAsync(item => item.Id == seeded.Order.Id)).Status);
        Assert.Equal(0, await db.MallRefunds.CountAsync());
    }

    [Fact]
    public async Task CreateRefund_RejectsDuplicateRefund()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var seeded = await SeedPaidOrderAsync(db, goodsAmount: 120m, freightAmount: 10m);
        var handler = new CreateMallRefundHandler(db);

        await handler.Handle(
            CreateCommand(seeded),
            CancellationToken.None);

        var pendingRefund = await db.MallRefunds.AsNoTracking()
            .FirstAsync(item => item.OrderId == seeded.Order.Id);

        // 退款处理中再次申请：由领域状态机拒绝
        var pendingException = await Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(CreateCommand(seeded), CancellationToken.None));
        Assert.Contains("当前订单状态不可申请退款", pendingException.Message);

        // 退款成功后再申请
        await new SimulateMallRefundHandler(db, DevelopmentEnvironment()).Handle(
            new SimulateMallRefundCommand
            {
                RefundId = pendingRefund.Id,
                Request = new SimulateMallRefundRequest { Success = true }
            },
            CancellationToken.None);

        var refundedException = await Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(CreateCommand(seeded), CancellationToken.None));
        Assert.Contains("当前订单状态不可申请退款", refundedException.Message);

        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.MallRefunds.CountAsync());
    }

    [Fact]
    public async Task SimulateRefund_Success_RestoresStockReleasesCouponAndSyncsStatus()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var seeded = await SeedPaidOrderAsync(
            db,
            goodsAmount: 120m,
            freightAmount: 10m,
            stock: 10,
            quantity: 2);

        var refund = await new CreateMallRefundHandler(db).Handle(
            CreateCommand(seeded),
            CancellationToken.None);

        var succeeded = await new SimulateMallRefundHandler(db, DevelopmentEnvironment()).Handle(
            new SimulateMallRefundCommand
            {
                RefundId = refund.Id,
                Request = new SimulateMallRefundRequest { Success = true }
            },
            CancellationToken.None);

        Assert.True(succeeded);

        db.ChangeTracker.Clear();

        var refundEntity = await db.MallRefunds.AsNoTracking()
            .FirstAsync(item => item.Id == refund.Id);
        Assert.Equal(MallRefundStatus.Success, refundEntity.Status);
        Assert.Equal(130m, refundEntity.Amount);

        var order = await db.MallOrders.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Order.Id);
        Assert.Equal(MallOrderStatus.Refunded, order.Status);
        Assert.Equal(130m, order.RefundedAmount);
        Assert.NotNull(order.RefundedAt);

        var payment = await db.MallPayments.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Payment.Id);
        Assert.Equal(MallPaymentStatus.Refunded, payment.Status);

        // 库存全量回补：下单扣减后剩 8，退款回补 2 后回到 10
        var sku = await db.MallSkus.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Sku.Id);
        Assert.Equal(10, sku.Stock);

        var stockLog = await db.MallStockLogs.AsNoTracking()
            .FirstAsync(item => item.RelatedOrderNo == seeded.Order.OrderNo);
        Assert.Equal(MallStockChangeType.RefundRestore, stockLog.ChangeType);
        Assert.Equal(8, stockLog.BeforeStock);
        Assert.Equal(10, stockLog.AfterStock);

        var coupon = await db.MallMemberCoupons.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Coupon.Id);
        Assert.Equal("claimed", coupon.Status);
        Assert.Null(coupon.UsedOrderNo);
        Assert.Null(coupon.UsedAt);
    }

    [Fact]
    public async Task ReturnChain_RefundsWholePaidAmountAndRestoresStock()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var seeded = await SeedPaidOrderAsync(
            db,
            goodsAmount: 120m,
            freightAmount: 10m,
            stock: 10,
            quantity: 2);

        seeded.Order.MarkShipped();
        await db.SaveChangesAsync();

        var returnEntity = await new CreateMallReturnHandler(db).Handle(
            new CreateMallReturnCommand
            {
                OrderId = seeded.Order.Id,
                Reason = "商品破损"
            },
            CancellationToken.None);

        // 退货同样是整单退，金额取订单实付（含运费）
        Assert.Equal(130m, returnEntity.Amount);

        await new ApproveMallReturnHandler(db).Handle(
            new ApproveMallReturnCommand
            {
                ReturnId = returnEntity.Id,
                Approve = true
            },
            CancellationToken.None);

        await new ConfirmMallReturnReceiptHandler(db).Handle(
            new ConfirmMallReturnReceiptCommand
            {
                ReturnId = returnEntity.Id
            },
            CancellationToken.None);

        db.ChangeTracker.Clear();

        var refund = await db.MallRefunds.AsNoTracking()
            .FirstAsync(item => item.OrderId == seeded.Order.Id);
        Assert.Equal(130m, refund.Amount);
        Assert.Equal(MallRefundStatus.Success, refund.Status);

        var order = await db.MallOrders.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Order.Id);
        Assert.Equal(MallOrderStatus.Returned, order.Status);
        Assert.Equal(130m, order.RefundedAmount);

        var payment = await db.MallPayments.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Payment.Id);
        Assert.Equal(MallPaymentStatus.Refunded, payment.Status);

        var sku = await db.MallSkus.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Sku.Id);
        Assert.Equal(10, sku.Stock);

        var coupon = await db.MallMemberCoupons.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Coupon.Id);
        Assert.Equal("claimed", coupon.Status);
        Assert.Null(coupon.UsedOrderNo);
    }

    [Fact]
    public async Task CreateRefund_TwoIndependentRequests_OnlyOneSucceeds()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db1 = factory.Context;
        var seeded = await SeedPaidOrderAsync(db1, goodsAmount: 120m, freightAmount: 10m);

        var first = await new CreateMallRefundHandler(db1).Handle(
            CreateCommand(seeded),
            CancellationToken.None);
        Assert.Equal(MallRefundStatus.Pending, first.Status);

        // 第二个请求使用独立 DbContext，模拟另一路请求看到订单已是退款中，由领域状态机拒绝
        await using var db2 = factory.CreateContext();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            new CreateMallRefundHandler(db2).Handle(CreateCommand(seeded), CancellationToken.None));

        Assert.Contains("当前订单状态不可申请退款", exception.Message);

        Assert.Equal(1, await db2.MallRefunds.CountAsync());
        Assert.Equal(
            MallOrderStatus.Refunding,
            (await db2.MallOrders.AsNoTracking().FirstAsync(item => item.Id == seeded.Order.Id)).Status);
    }

    [Fact]
    public async Task SimulateRefund_Failure_RollsBackToPaidAndAllowsRetry()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var seeded = await SeedPaidOrderAsync(db, goodsAmount: 120m, freightAmount: 10m);

        var refund = await new CreateMallRefundHandler(db).Handle(
            CreateCommand(seeded),
            CancellationToken.None);

        var failed = await new SimulateMallRefundHandler(db, DevelopmentEnvironment()).Handle(
            new SimulateMallRefundCommand
            {
                RefundId = refund.Id,
                Request = new SimulateMallRefundRequest { Success = false }
            },
            CancellationToken.None);
        Assert.True(failed);

        db.ChangeTracker.Clear();

        var order = await db.MallOrders.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Order.Id);
        Assert.Equal(MallOrderStatus.Paid, order.Status);
        Assert.Equal(0m, order.RefundedAmount);

        var failedRefund = await db.MallRefunds.AsNoTracking()
            .FirstAsync(item => item.Id == refund.Id);
        Assert.Equal(MallRefundStatus.Failed, failedRefund.Status);

        // 失败回退后订单回到已支付，可以再次发起退款
        var retry = await new CreateMallRefundHandler(db).Handle(
            CreateCommand(seeded),
            CancellationToken.None);
        Assert.Equal(MallRefundStatus.Pending, retry.Status);

        await new SimulateMallRefundHandler(db, DevelopmentEnvironment()).Handle(
            new SimulateMallRefundCommand
            {
                RefundId = retry.Id,
                Request = new SimulateMallRefundRequest { Success = true }
            },
            CancellationToken.None);

        db.ChangeTracker.Clear();

        order = await db.MallOrders.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Order.Id);
        Assert.Equal(MallOrderStatus.Refunded, order.Status);
        Assert.Equal(130m, order.RefundedAmount);
        Assert.Equal(
            10,
            (await db.MallSkus.AsNoTracking().FirstAsync(item => item.Id == seeded.Sku.Id)).Stock);
    }

    [Fact]
    public async Task SimulateRefund_Success_DoesNotRollBackAndRestoresOnlyOnce()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var seeded = await SeedPaidOrderAsync(db, goodsAmount: 120m, freightAmount: 10m);

        var refund = await new CreateMallRefundHandler(db).Handle(
            CreateCommand(seeded),
            CancellationToken.None);

        await new SimulateMallRefundHandler(db, DevelopmentEnvironment()).Handle(
            new SimulateMallRefundCommand
            {
                RefundId = refund.Id,
                Request = new SimulateMallRefundRequest { Success = true }
            },
            CancellationToken.None);

        // 已成功的退款单再次模拟（包括失败）都会被拒绝，订单状态不回退
        await Assert.ThrowsAsync<BusinessException>(() =>
            new SimulateMallRefundHandler(db, DevelopmentEnvironment()).Handle(
                new SimulateMallRefundCommand
                {
                    RefundId = refund.Id,
                    Request = new SimulateMallRefundRequest { Success = false }
                },
                CancellationToken.None));

        db.ChangeTracker.Clear();

        var order = await db.MallOrders.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Order.Id);
        Assert.Equal(MallOrderStatus.Refunded, order.Status);
        Assert.Equal(130m, order.RefundedAmount);

        Assert.Equal(
            10,
            (await db.MallSkus.AsNoTracking().FirstAsync(item => item.Id == seeded.Sku.Id)).Stock);
        Assert.Equal(1, await db.MallStockLogs.CountAsync());
        Assert.Equal(1, await db.MallRefunds.CountAsync());
    }

    [Fact]
    public async Task SimulateRefund_Unshipped_RestoresProductAndGiftStockImmediately()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var seeded = await SeedPaidOrderAsync(
            db,
            goodsAmount: 120m,
            freightAmount: 10m,
            stock: 10,
            quantity: 2,
            withGift: true);

        Assert.NotNull(seeded.GiftSku);
        Assert.True(await db.MallOrderItems.AsNoTracking()
            .AnyAsync(item => item.OrderId == seeded.Order.Id && item.IsGift));

        var refund = await new CreateMallRefundHandler(db).Handle(
            CreateCommand(seeded),
            CancellationToken.None);

        await new SimulateMallRefundHandler(db, DevelopmentEnvironment()).Handle(
            new SimulateMallRefundCommand
            {
                RefundId = refund.Id,
                Request = new SimulateMallRefundRequest { Success = true }
            },
            CancellationToken.None);

        db.ChangeTracker.Clear();

        var order = await db.MallOrders.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Order.Id);
        Assert.Equal(MallOrderStatus.Refunded, order.Status);

        // 商品与赠品行一并回补：8 → 10、4 → 5
        Assert.Equal(
            10,
            (await db.MallSkus.AsNoTracking().FirstAsync(item => item.Id == seeded.Sku.Id)).Stock);
        Assert.Equal(
            5,
            (await db.MallSkus.AsNoTracking().FirstAsync(item => item.Id == seeded.GiftSku!.Id)).Stock);

        var stockLogs = await db.MallStockLogs.AsNoTracking()
            .Where(log => log.RelatedOrderNo == seeded.Order.OrderNo)
            .ToListAsync();
        Assert.Equal(2, stockLogs.Count);
        Assert.All(stockLogs, log => Assert.Equal(MallStockChangeType.RefundRestore, log.ChangeType));
    }

    [Fact]
    public async Task SimulateRefund_Shipped_DefersRestoreUntilReturnReceipt()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var seeded = await SeedPaidOrderAsync(
            db,
            goodsAmount: 120m,
            freightAmount: 10m,
            stock: 10,
            quantity: 2,
            withGift: true);

        seeded.Order.MarkShipped();
        await db.SaveChangesAsync();

        var refund = await new CreateMallRefundHandler(db).Handle(
            CreateCommand(seeded),
            CancellationToken.None);

        await new SimulateMallRefundHandler(db, DevelopmentEnvironment()).Handle(
            new SimulateMallRefundCommand
            {
                RefundId = refund.Id,
                Request = new SimulateMallRefundRequest { Success = true }
            },
            CancellationToken.None);

        db.ChangeTracker.Clear();

        var order = await db.MallOrders.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Order.Id);
        Assert.Equal(MallOrderStatus.Refunded, order.Status);
        Assert.Equal(130m, order.RefundedAmount);

        // 已发货退款不立即回补，库存维持在下单后水位
        Assert.Equal(
            8,
            (await db.MallSkus.AsNoTracking().FirstAsync(item => item.Id == seeded.Sku.Id)).Stock);
        Assert.Equal(
            4,
            (await db.MallSkus.AsNoTracking().FirstAsync(item => item.Id == seeded.GiftSku!.Id)).Stock);
        Assert.Equal(0, await db.MallStockLogs.CountAsync());

        // 退款后仍可发起退货，等入库确认再回补
        var returnEntity = await new CreateMallReturnHandler(db).Handle(
            new CreateMallReturnCommand { OrderId = seeded.Order.Id, Reason = "商品破损" },
            CancellationToken.None);
        Assert.Equal(130m, returnEntity.Amount);

        await new ApproveMallReturnHandler(db).Handle(
            new ApproveMallReturnCommand { ReturnId = returnEntity.Id, Approve = true },
            CancellationToken.None);

        await new ConfirmMallReturnReceiptHandler(db).Handle(
            new ConfirmMallReturnReceiptCommand { ReturnId = returnEntity.Id },
            CancellationToken.None);

        db.ChangeTracker.Clear();

        order = await db.MallOrders.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Order.Id);
        Assert.Equal(MallOrderStatus.Returned, order.Status);

        Assert.Equal(
            10,
            (await db.MallSkus.AsNoTracking().FirstAsync(item => item.Id == seeded.Sku.Id)).Stock);
        Assert.Equal(
            5,
            (await db.MallSkus.AsNoTracking().FirstAsync(item => item.Id == seeded.GiftSku!.Id)).Stock);

        var stockLogs = await db.MallStockLogs.AsNoTracking()
            .Where(log => log.RelatedOrderNo == seeded.Order.OrderNo)
            .ToListAsync();
        Assert.Equal(2, stockLogs.Count);
        Assert.All(stockLogs, log => Assert.Equal(MallStockChangeType.ReturnRestore, log.ChangeType));

        // 退款单只有一笔，入库确认不重复建退款单
        Assert.Equal(1, await db.MallRefunds.CountAsync());
        var payment = await db.MallPayments.AsNoTracking()
            .FirstAsync(item => item.Id == seeded.Payment.Id);
        Assert.Equal(MallPaymentStatus.Refunded, payment.Status);

        // 重复确认收货不重复回补
        await Assert.ThrowsAsync<BusinessException>(() =>
            new ConfirmMallReturnReceiptHandler(db).Handle(
                new ConfirmMallReturnReceiptCommand { ReturnId = returnEntity.Id },
                CancellationToken.None));

        db.ChangeTracker.Clear();

        Assert.Equal(
            10,
            (await db.MallSkus.AsNoTracking().FirstAsync(item => item.Id == seeded.Sku.Id)).Stock);
        Assert.Equal(
            5,
            (await db.MallSkus.AsNoTracking().FirstAsync(item => item.Id == seeded.GiftSku!.Id)).Stock);
        Assert.Equal(2, await db.MallStockLogs.CountAsync());
        Assert.Equal(1, await db.MallRefunds.CountAsync());
    }

    private static CreateMallRefundCommand CreateCommand(SeededOrder seeded)
    {
        return new CreateMallRefundCommand
        {
            OrderId = seeded.Order.Id,
            MemberId = seeded.Order.MemberId,
            Request = new CreateMallRefundRequest { Reason = "不想要了" }
        };
    }

    private static IHostEnvironment DevelopmentEnvironment()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(Environments.Development);
        return environment.Object;
    }

    /// <summary>
    /// 造一笔已支付订单：下单先扣减库存（10 → 8），支付成功，券已占用。
    /// </summary>
    private static async Task<SeededOrder> SeedPaidOrderAsync(
        AppDbContext db,
        decimal goodsAmount,
        decimal freightAmount,
        int stock = 10,
        int quantity = 2,
        bool withGift = false)
    {
        var orderNo = $"ORD{Guid.NewGuid():N}";
        var store = MallStore.Create($"ST{Guid.NewGuid():N}", "测试门店", "测试地址", true);
        var product = MallProduct.Create(
            store.Id,
            $"PD{Guid.NewGuid():N}",
            "测试商品",
            "测试商品",
            null,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            true);
        var member = MallMember.Register($"139{Guid.NewGuid().ToString("N")[..8]}", "hash", "测试会员");
        var sku = MallSku.Create(
            product.Id,
            $"SKU{Guid.NewGuid():N}",
            "500g 装",
            goodsAmount / quantity,
            500,
            stock,
            0,
            true);

        var order = MallOrder.Create(
            orderNo,
            member.Id,
            "张三",
            "13800000000",
            "测试地址",
            goodsAmount,
            0m,
            0m,
            freightAmount,
            quantity * 500,
            string.Empty);

        var orderItem = MallOrderItem.Create(
            order.Id,
            sku.Id,
            product.Id,
            "测试商品",
            sku.SkuCode,
            sku.SpecName,
            goodsAmount / quantity,
            quantity,
            500);

        // 赠品行：与商品同订单落库为 0 元明细，回补时随商品行一并处理
        var giftSku = withGift
            ? MallSku.Create(
                product.Id,
                $"GIFT{Guid.NewGuid():N}",
                "赠品规格",
                0m,
                500,
                5,
                0,
                true)
            : null;

        var giftItem = giftSku is null
            ? null
            : MallOrderItem.Create(
                order.Id,
                giftSku.Id,
                product.Id,
                "测试赠品",
                giftSku.SkuCode,
                giftSku.SpecName,
                0m,
                1,
                500,
                isGift: true);

        var payment = MallPayment.Create(
            $"PAY{Guid.NewGuid():N}",
            order.Id,
            goodsAmount + freightAmount,
            "Mock");

        var coupon = MallMemberCoupon.Issue(
            Guid.NewGuid(),
            member.Id,
            $"CPN{Guid.NewGuid():N}",
            DateTime.Now.AddDays(7));

        db.MallStores.Add(store);
        db.MallProducts.Add(product);
        db.MallMembers.Add(member);
        db.MallSkus.Add(sku);
        db.MallOrders.Add(order);
        db.MallOrderItems.Add(orderItem);
        if (giftSku is not null && giftItem is not null)
        {
            db.MallSkus.Add(giftSku);
            db.MallOrderItems.Add(giftItem);
        }

        db.MallPayments.Add(payment);
        db.MallMemberCoupons.Add(coupon);
        await db.SaveChangesAsync();

        payment.MarkSuccess($"TXN{Guid.NewGuid():N}");
        order.MarkPaid(goodsAmount + freightAmount);
        coupon.Use(orderNo);
        await db.SaveChangesAsync();

        // 模拟下单时的库存扣减，退款回补后应回到 10
        await db.MallSkus
            .Where(item => item.Id == sku.Id)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(item => item.Stock, item => item.Stock - quantity));

        if (giftSku is not null)
        {
            await db.MallSkus
                .Where(item => item.Id == giftSku.Id)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(item => item.Stock, item => item.Stock - 1));
        }

        return new SeededOrder(order, sku, payment, coupon, giftSku);
    }

    private sealed record SeededOrder(
        MallOrder Order,
        MallSku Sku,
        MallPayment Payment,
        MallMemberCoupon Coupon,
        MallSku? GiftSku = null);
}
