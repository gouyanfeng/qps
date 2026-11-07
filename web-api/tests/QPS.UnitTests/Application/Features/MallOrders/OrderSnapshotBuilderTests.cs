using System.Text.Json;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.Shared.Exceptions;
using Xunit;

namespace QPS.UnitTests.Application.Features.MallOrders;

/// <summary>
/// 订单快照构建：金额恒等式、行级促销额、结构化促销快照与恒写原价，
/// 覆盖有促销无券、有券无促销、两者都有、赠品与运费五种组合。
/// </summary>
public class OrderSnapshotBuilderTests
{
    [Fact]
    public void BuildProductItem_WithPromotionWithoutCoupon_RecordsStructuredSnapshot()
    {
        var product = CreateProduct();
        var sku = CreateSku(product.Id, price: 100m);
        var order = BuildOrder(goodsAmount: 200m, promotionDiscount: 40m, couponDiscount: 0m, freight: 10m);
        var appliedItem = new SkuOrderRow
        {
            SkuId = sku.Id,
            ProductId = product.Id,
            OriginalPrice = 100m,
            CurrentPrice = 80m,
            Quantity = 2,
            WeightGram = 100,
            AppliedPromotions = new List<AppliedPromotionRecord>
            {
                new()
                {
                    PromotionId = Guid.NewGuid(),
                    PromotionName = "满 200 减 40",
                    ActionType = "DiscountAmount",
                    DiscountAmount = 40m
                }
            }
        };

        var item = OrderSnapshotBuilder.BuildProductItem(order, sku, product, appliedItem);

        Assert.Equal(170m, order.PayableAmount);
        Assert.Equal(80m, item.UnitPrice);
        Assert.Equal(100m, item.OriginalUnitPrice);
        Assert.Equal(40m, item.PromotionDiscountAmount);
        // 主促销只存名称，不把「名称(-金额)」拼进数据
        Assert.Equal("满 200 减 40", item.PromotionName);
        Assert.DoesNotContain("(-", item.PromotionName);
        Assert.DoesNotContain("(-", item.PromotionSnapshot);

        var entry = AssertSingleSnapshotEntry(item);
        Assert.Equal("满 200 减 40", entry.GetProperty("name").GetString());
        Assert.Equal("DiscountAmount", entry.GetProperty("actionType").GetString());
        Assert.Equal(40m, entry.GetProperty("amount").GetDecimal());

        OrderSnapshotBuilder.EnsureLinePromotionTotal(order, new[] { item });
    }

    [Fact]
    public void BuildProductItem_WithCouponWithoutPromotion_KeepsOriginalPriceEqualToUnitPrice()
    {
        var product = CreateProduct();
        var sku = CreateSku(product.Id, price: 50m);
        var couponId = Guid.NewGuid();
        var order = BuildOrder(
            goodsAmount: 100m,
            promotionDiscount: 0m,
            couponDiscount: 10m,
            freight: 0m,
            couponId: couponId,
            couponName: "新人立减 10 元");
        var appliedItem = new SkuOrderRow
        {
            SkuId = sku.Id,
            ProductId = product.Id,
            OriginalPrice = 50m,
            CurrentPrice = 50m,
            Quantity = 2,
            WeightGram = 100
        };

        var item = OrderSnapshotBuilder.BuildProductItem(order, sku, product, appliedItem);

        Assert.Equal(90m, order.PayableAmount);
        Assert.Equal(couponId, order.CouponId);
        Assert.Equal("新人立减 10 元", order.CouponName);
        Assert.Equal(10m, order.CouponDiscountAmount);
        Assert.Equal(item.UnitPrice, item.OriginalUnitPrice);
        Assert.Equal(0m, item.PromotionDiscountAmount);
        Assert.Equal("[]", item.PromotionSnapshot);
        Assert.Equal(string.Empty, item.PromotionName);
        OrderSnapshotBuilder.EnsureLinePromotionTotal(order, new[] { item });
    }

    [Fact]
    public void BuildProductItem_WithPromotionAndCoupon_SumsBothDiscounts()
    {
        var product = CreateProduct();
        var sku = CreateSku(product.Id, price: 100m);
        var order = BuildOrder(
            goodsAmount: 200m,
            promotionDiscount: 40m,
            couponDiscount: 20m,
            freight: 12m,
            couponId: Guid.NewGuid(),
            couponName: "店铺券");
        var appliedItem = new SkuOrderRow
        {
            SkuId = sku.Id,
            ProductId = product.Id,
            OriginalPrice = 100m,
            CurrentPrice = 80m,
            Quantity = 2,
            WeightGram = 100,
            AppliedPromotions = new List<AppliedPromotionRecord>
            {
                new()
                {
                    PromotionId = Guid.NewGuid(),
                    PromotionName = "八折",
                    ActionType = "DiscountRate",
                    DiscountAmount = 40m
                }
            }
        };

        var item = OrderSnapshotBuilder.BuildProductItem(order, sku, product, appliedItem);

        // 商品 200 − 促销 40 − 券 20 + 运费 12 = 152
        Assert.Equal(152m, order.PayableAmount);
        Assert.Equal(40m, item.PromotionDiscountAmount);
        Assert.Equal(40m, order.PromotionDiscountAmount);
        Assert.Equal(20m, order.CouponDiscountAmount);
        OrderSnapshotBuilder.EnsureLinePromotionTotal(order, new[] { item });
    }

    [Fact]
    public void BuildGiftItem_WritesZeroAmountGiftSnapshot()
    {
        var product = CreateProduct();
        var sku = CreateSku(product.Id, price: 30m);
        var promotionId = Guid.NewGuid();
        var order = BuildOrder(goodsAmount: 100m, promotionDiscount: 0m, couponDiscount: 0m, freight: 0m);
        var gift = new GiftSkuRow
        {
            SkuId = sku.Id,
            Quantity = 1,
            PromotionId = promotionId,
            PromotionName = "买二赠一"
        };

        var item = OrderSnapshotBuilder.BuildGiftItem(order, sku, product, gift);

        Assert.True(item.IsGift);
        Assert.Equal(0m, item.UnitPrice);
        Assert.Equal(0m, item.OriginalUnitPrice);
        Assert.Equal(0m, item.ItemAmount);
        Assert.Equal(0m, item.PromotionDiscountAmount);
        Assert.Equal(promotionId, item.PromotionId);
        Assert.Equal("买二赠一", item.PromotionName);

        var entry = AssertSingleSnapshotEntry(item);
        Assert.Equal("Gift", entry.GetProperty("actionType").GetString());
        Assert.Equal(0m, entry.GetProperty("amount").GetDecimal());

        // 赠品不参与订单级促销优惠合计
        OrderSnapshotBuilder.EnsureLinePromotionTotal(order, new[] { item });
    }

    [Fact]
    public void BuildOrder_WithFreight_IncludesFreightInPayable()
    {
        var order = BuildOrder(goodsAmount: 100m, promotionDiscount: 0m, couponDiscount: 0m, freight: 15m);

        Assert.Equal(115m, order.PayableAmount);
        Assert.Equal(15m, order.FreightAmount);
        Assert.Equal(100m, order.GoodsAmount);

        // 金额不自洽时明确抛出，不静默展示
        Assert.Throws<BusinessException>(() =>
            MallOrder.EnsureAmountIdentity(order.OrderNo, 100m, 0m, 0m, 15m, 120m));
    }

    [Fact]
    public void EnsureLinePromotionTotal_ThrowsWhenLineTotalDiffersFromOrder()
    {
        var product = CreateProduct();
        var sku = CreateSku(product.Id, price: 100m);
        var order = BuildOrder(goodsAmount: 200m, promotionDiscount: 40m, couponDiscount: 0m, freight: 0m);
        var appliedItem = new SkuOrderRow
        {
            SkuId = sku.Id,
            ProductId = product.Id,
            OriginalPrice = 100m,
            CurrentPrice = 90m,
            Quantity = 2,
            WeightGram = 100,
            AppliedPromotions = new List<AppliedPromotionRecord>
            {
                new()
                {
                    PromotionId = Guid.NewGuid(),
                    PromotionName = "九折",
                    ActionType = "DiscountRate",
                    DiscountAmount = 20m
                }
            }
        };
        var item = OrderSnapshotBuilder.BuildProductItem(order, sku, product, appliedItem);

        var exception = Assert.Throws<BusinessException>(() =>
            OrderSnapshotBuilder.EnsureLinePromotionTotal(order, new[] { item }));

        Assert.Contains("促销优惠与明细不一致", exception.Message);
    }

    private static MallOrder BuildOrder(
        decimal goodsAmount,
        decimal promotionDiscount,
        decimal couponDiscount,
        decimal freight,
        Guid? couponId = null,
        string couponName = "")
    {
        return OrderSnapshotBuilder.BuildOrder(new OrderHeaderSnapshot
        {
            OrderNo = "ORD202611070001",
            MemberId = Guid.NewGuid(),
            ReceiverName = "张三",
            ReceiverPhone = "13601758260",
            ReceiverAddress = "浙江省杭州市西湖区文三路 288 号",
            GoodsAmount = goodsAmount,
            PromotionDiscountAmount = promotionDiscount,
            CouponDiscountAmount = couponDiscount,
            CouponId = couponId,
            CouponName = couponName,
            FreightAmount = freight,
            TotalWeightGram = 200,
            Remark = string.Empty
        });
    }

    private static MallProduct CreateProduct()
    {
        return MallProduct.Create(
            Guid.NewGuid(),
            "P-001",
            "黄芪",
            string.Empty,
            null,
            "/static/images/herbs.jpg",
            "[]",
            string.Empty,
            0,
            true);
    }

    private static MallSku CreateSku(Guid productId, decimal price)
    {
        return MallSku.Create(
            productId,
            "SKU-001",
            "500g 装",
            price,
            weightGram: 100,
            stock: 10,
            warnStock: 0,
            isActive: true);
    }

    private static JsonElement AssertSingleSnapshotEntry(MallOrderItem item)
    {
        using var document = JsonDocument.Parse(item.PromotionSnapshot);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Equal(1, document.RootElement.GetArrayLength());
        return document.RootElement[0].Clone();
    }
}
