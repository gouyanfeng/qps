using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.Persistence.Database;
using QPS.UnitTests.Common;
using Xunit;

namespace QPS.UnitTests.Application.Features.MallOrders;

/// <summary>
/// 价格明细按优惠类型拆分：折后优惠（DiscountRate）与满减优惠（DiscountAmount）必须可对账，
/// 分项之和等于促销优惠合计，并与实付金额吻合；买赠不产生金额。
/// </summary>
public class MallOrderPreviewDiscountSplitTests
{
    [Fact]
    public async Task Preview_OnlyRate_ReturnsRateAmountAndNoAmountDiscount()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var (sku, address, memberId, productId) = await SeedAsync(db, price: 200m);

        AddPromotion(db, CreateRate(productId, 0.8m), "Product", productId);
        await db.SaveChangesAsync();

        var preview = await PreviewAsync(db, sku.Id, address.Id, memberId, quantity: 2);

        Assert.Equal(400m, preview.GoodsAmount);
        Assert.Equal(80m, preview.RateDiscountAmount);
        Assert.Equal(0m, preview.AmountDiscountAmount);
        Assert.Equal(80m, preview.PromotionDiscountAmount);
        Assert.Equal(320m, preview.PayableAmount);
    }

    [Fact]
    public async Task Preview_OnlyAmount_ReturnsAmountDiscountAndNoRateAmount()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var (sku, address, memberId, productId) = await SeedAsync(db, price: 200m);

        AddPromotion(db, CreateAmount(productId, threshold: 200m, amount: 30m), "Product", productId);
        await db.SaveChangesAsync();

        var preview = await PreviewAsync(db, sku.Id, address.Id, memberId, quantity: 2);

        Assert.Equal(400m, preview.GoodsAmount);
        Assert.Equal(0m, preview.RateDiscountAmount);
        Assert.Equal(30m, preview.AmountDiscountAmount);
        Assert.Equal(30m, preview.PromotionDiscountAmount);
        Assert.Equal(370m, preview.PayableAmount);
    }

    [Fact]
    public async Task Preview_RateAmountAndGift_GiftHasNoMoneyAndSplitsSumToTotal()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var (sku, address, memberId, productId) = await SeedAsync(db, price: 200m);

        AddPromotion(db, CreateRate(productId, 0.8m), "Product", productId);
        AddPromotion(db, CreateAmount(productId, threshold: 200m, amount: 30m), "Product", productId);
        AddPromotion(db, CreateGift(productId), "Product", productId);
        await db.SaveChangesAsync();

        var preview = await PreviewAsync(db, sku.Id, address.Id, memberId, quantity: 2);

        Assert.Equal(80m, preview.RateDiscountAmount);
        Assert.Equal(30m, preview.AmountDiscountAmount);
        Assert.Equal(110m, preview.PromotionDiscountAmount);
        Assert.Equal(preview.PromotionDiscountAmount, preview.RateDiscountAmount + preview.AmountDiscountAmount);
        Assert.Equal(290m, preview.PayableAmount);
    }

    [Fact]
    public async Task Preview_RateAmountAndCoupon_SplitsSumToPromotionTotalAndPayableMatches()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var (sku, address, memberId, productId) = await SeedAsync(db, price: 200m);

        AddPromotion(db, CreateRate(productId, 0.8m), "Product", productId);
        AddPromotion(db, CreateAmount(productId, threshold: 200m, amount: 30m), "Product", productId);

        var coupon = MallCoupon.Create(
            "TEST-COUPON",
            "测试券",
            "fixed",
            0m,
            20m,
            null,
            100,
            1,
            7,
            null,
            null,
            "all",
            null);
        var memberCoupon = MallMemberCoupon.Issue(
            coupon.Id,
            memberId,
            $"TEST-{Guid.NewGuid():N}",
            DateTime.Now.AddDays(7));
        db.MallCoupons.Add(coupon);
        db.MallMemberCoupons.Add(memberCoupon);
        await db.SaveChangesAsync();

        var preview = await PreviewAsync(
            db,
            sku.Id,
            address.Id,
            memberId,
            quantity: 2,
            memberCouponId: memberCoupon.Id);

        Assert.Equal(80m, preview.RateDiscountAmount);
        Assert.Equal(30m, preview.AmountDiscountAmount);
        Assert.Equal(110m, preview.PromotionDiscountAmount);
        Assert.Equal(preview.PromotionDiscountAmount, preview.RateDiscountAmount + preview.AmountDiscountAmount);
        Assert.Equal(20m, preview.CouponDiscountAmount);
        // 400 - 折后 80 - 满减 30 - 券 20 + 运费 0
        Assert.Equal(270m, preview.PayableAmount);
        Assert.Equal(
            preview.GoodsAmount - preview.RateDiscountAmount - preview.AmountDiscountAmount - preview.CouponDiscountAmount + preview.FreightAmount,
            preview.PayableAmount);
    }

    private static MallPromotion CreateRate(Guid productId, decimal rate)
    {
        return MallPromotion.Create(
            "测试折扣",
            string.Empty,
            MallPromotionType.DiscountRate,
            0m,
            "DiscountRate",
            $$"""{"rate":{{rate}}}""",
            "Product",
            null,
            null,
            null,
            true);
    }

    private static MallPromotion CreateAmount(Guid productId, decimal threshold, decimal amount)
    {
        return MallPromotion.Create(
            "测试满减",
            string.Empty,
            MallPromotionType.DiscountAmount,
            threshold,
            "DiscountAmount",
            $$"""{"amount":{{amount}}}""",
            "Product",
            null,
            null,
            null,
            true);
    }

    private static MallPromotion CreateGift(Guid productId)
    {
        var giftSkuId = Guid.NewGuid();
        return MallPromotion.Create(
            "测试买赠",
            string.Empty,
            MallPromotionType.Gift,
            0m,
            "Gift",
            $$"""{"skuId":"{{giftSkuId}}","quantity":1}""",
            "Product",
            null,
            null,
            null,
            true);
    }

    private static void AddPromotion(
        AppDbContext db,
        MallPromotion promotion,
        string scopeType,
        Guid scopeTargetId)
    {
        db.MallPromotions.Add(promotion);
        db.MallPromotionScopes.Add(MallPromotionScope.Create(promotion.Id, scopeType, scopeTargetId));
    }

    private static async Task<MallOrderPreviewDto> PreviewAsync(
        AppDbContext db,
        Guid skuId,
        Guid addressId,
        Guid memberId,
        int quantity,
        Guid? memberCouponId = null)
    {
        var handler = CreateHandler(db);
        return await handler.Handle(
            new PreviewMallOrderQuery
            {
                MemberId = memberId,
                Request = new PreviewMallOrderRequest
                {
                    AddressId = addressId,
                    MemberCouponId = memberCouponId,
                    Items = { new PreviewMallOrderItem { SkuId = skuId, Quantity = quantity } }
                }
            },
            CancellationToken.None);
    }

    private static PreviewMallOrderHandler CreateHandler(AppDbContext db)
    {
        var engine = new MallPromotionEngine(new ActionStrategyRegistry(
            new IActionStrategy[]
            {
                new DiscountAmountAction(),
                new DiscountRateAction(),
                new GiftAction(),
                new FreightFreeAction()
            }));

        var previewService = new MallPromotionPreviewService(
            db,
            engine,
            new MallPromotionContextBuilder(db));

        return new PreviewMallOrderHandler(previewService, new MallCouponService(db), db);
    }

    private static async Task<(MallSku Sku, MallAddress Address, Guid MemberId, Guid ProductId)> SeedAsync(
        AppDbContext db,
        decimal price)
    {
        var template = MallFreightTemplate.Create(
            "默认运费",
            FreightBillingType.Weight,
            isDefault: true,
            isActive: true);
        db.MallFreightTemplates.Add(template);
        db.MallFreightRegions.Add(MallFreightRegion.Create(
            template.Id,
            "全国",
            "全国",
            null,
            1000,
            10m,
            1000,
            5m,
            99m,
            null,
            isDefaultRegion: true));

        var store = MallStore.Create($"ST{Guid.NewGuid():N}", "分项金额测试门店", "测试地址", true);
        db.MallStores.Add(store);

        var product = MallProduct.Create(
            store.Id,
            $"HC-SPLIT-{Guid.NewGuid():N}",
            "分项金额测试商品",
            string.Empty,
            null,
            string.Empty,
            string.Empty,
            string.Empty,
            1,
            true,
            false,
            template.Id);
        var sku = MallSku.Create(
            product.Id,
            $"HC-SPLIT-SKU-{Guid.NewGuid():N}",
            "规格",
            price,
            500,
            100,
            5,
            true);

        db.MallProducts.Add(product);
        db.MallSkus.Add(sku);

        var member = MallMember.Register($"139{Guid.NewGuid().ToString("N")[..8]}", "hash", "分项金额测试会员");
        db.MallMembers.Add(member);

        var address = MallAddress.Create(
            member.Id,
            "测试收货人",
            "13800000000",
            "广东省",
            "广州市",
            "天河区",
            "测试路 1 号",
            true);
        db.MallAddresses.Add(address);

        await db.SaveChangesAsync();

        return (sku, address, member.Id, product.Id);
    }
}
