using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.Shared.Exceptions;
using QPS.UnitTests.Common;
using Xunit;

namespace QPS.UnitTests.Application.Features.MallOrders;

/// <summary>预览与下单共用运费口径：预览必须返回运费并计入应付金额。</summary>
public class MallOrderPreviewFreightTests
{
    [Fact]
    public async Task Preview_WithAddress_AddsFreightToPayableAmount()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;

        var (sku, address, memberId) = await SeedAsync(db, price: 68m);

        var handler = CreateHandler(db);

        var preview = await handler.Handle(
            new PreviewMallOrderQuery
            {
                MemberId = memberId,
                Request = new PreviewMallOrderRequest
                {
                    AddressId = address.Id,
                    Items = { new PreviewMallOrderItem { SkuId = sku.Id, Quantity = 1 } }
                }
            },
            CancellationToken.None);

        Assert.Equal(68m, preview.GoodsAmount);
        Assert.Equal(0m, preview.PromotionDiscountAmount);
        Assert.Equal(10m, preview.FreightAmount);
        Assert.Equal(78m, preview.PayableAmount);
    }

    [Fact]
    public async Task Preview_WithAddressMeetingFreeShippingThreshold_FreightIsZero()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;

        var (sku, address, memberId) = await SeedAsync(db, price: 128m);

        var handler = CreateHandler(db);

        var preview = await handler.Handle(
            new PreviewMallOrderQuery
            {
                MemberId = memberId,
                Request = new PreviewMallOrderRequest
                {
                    AddressId = address.Id,
                    Items = { new PreviewMallOrderItem { SkuId = sku.Id, Quantity = 1 } }
                }
            },
            CancellationToken.None);

        Assert.Equal(0m, preview.FreightAmount);
        Assert.Equal(128m, preview.PayableAmount);
    }

    [Fact]
    public async Task Preview_WithoutAddress_Returns400()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;

        var (sku, _, memberId) = await SeedAsync(db, price: 68m);

        var handler = CreateHandler(db);

        var missing = await Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(
                new PreviewMallOrderQuery
                {
                    MemberId = memberId,
                    Request = new PreviewMallOrderRequest
                    {
                        Items = { new PreviewMallOrderItem { SkuId = sku.Id, Quantity = 1 } }
                    }
                },
                CancellationToken.None));
        Assert.Equal(400, missing.ErrorCode);
        Assert.Contains("请选择收货地址", missing.Message);

        var empty = await Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(
                new PreviewMallOrderQuery
                {
                    MemberId = memberId,
                    Request = new PreviewMallOrderRequest
                    {
                        AddressId = Guid.Empty,
                        Items = { new PreviewMallOrderItem { SkuId = sku.Id, Quantity = 1 } }
                    }
                },
                CancellationToken.None));
        Assert.Equal(400, empty.ErrorCode);
        Assert.Contains("请选择收货地址", empty.Message);
    }

    [Fact]
    public async Task Preview_WithOtherMembersAddress_Returns400()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;

        var (sku, _, memberId) = await SeedAsync(db, price: 68m);

        var otherMember = MallMember.Register($"137{Guid.NewGuid().ToString("N")[..8]}", "hash", "其他会员");
        db.MallMembers.Add(otherMember);
        var otherAddress = MallAddress.Create(
            otherMember.Id,
            "其他收货人",
            "13700000000",
            "广东省",
            "广州市",
            "天河区",
            "其他地址 1 号",
            true);
        db.MallAddresses.Add(otherAddress);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(
                new PreviewMallOrderQuery
                {
                    MemberId = memberId,
                    Request = new PreviewMallOrderRequest
                    {
                        AddressId = otherAddress.Id,
                        Items = { new PreviewMallOrderItem { SkuId = sku.Id, Quantity = 1 } }
                    }
                },
                CancellationToken.None));

        Assert.Equal(400, exception.ErrorCode);
        Assert.Contains("不属于当前会员", exception.Message);
    }

    private static PreviewMallOrderHandler CreateHandler(QPS.Persistence.Database.AppDbContext db)
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

    private static async Task<(MallSku Sku, MallAddress Address, Guid MemberId)> SeedAsync(
        QPS.Persistence.Database.AppDbContext db,
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

        var store = MallStore.Create($"ST{Guid.NewGuid():N}", "运费测试门店", "测试地址", true);
        db.MallStores.Add(store);

        var product = MallProduct.Create(
            store.Id,
            $"HC-FREIGHT-{Guid.NewGuid():N}",
            "运费测试商品",
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
            $"HC-FREIGHT-SKU-{Guid.NewGuid():N}",
            "规格",
            price,
            500,
            100,
            5,
            true);

        db.MallProducts.Add(product);
        db.MallSkus.Add(sku);

        var member = MallMember.Register($"139{Guid.NewGuid().ToString("N")[..8]}", "hash", "运费测试会员");
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

        return (sku, address, member.Id);
    }
}
