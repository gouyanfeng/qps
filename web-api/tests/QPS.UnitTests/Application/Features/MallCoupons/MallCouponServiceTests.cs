using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using Xunit;

namespace QPS.UnitTests.Application.Features.MallCoupons;

/// <summary>
/// 券可用性判定：覆盖状态、有效期、门槛与适用范围，确保确认页、预览与下单共用同一口径。
/// </summary>
public class MallCouponServiceTests
{
    private static readonly Guid MemberId = Guid.NewGuid();

    private static MallMemberCoupon Issued(Guid couponId, DateTime? expireAt = null)
    {
        return MallMemberCoupon.Issue(
            couponId,
            MemberId,
            "TEST-COUPON",
            expireAt ?? DateTime.Now.AddDays(7));
    }

    private static MallCoupon Template(
        decimal thresholdAmount,
        decimal discountAmount,
        decimal? maxDiscount,
        string couponType,
        string scopeType,
        string? scopeValue)
    {
        return MallCoupon.Create(
            "TEST",
            "测试券",
            couponType,
            thresholdAmount,
            discountAmount,
            maxDiscount,
            100,
            1,
            7,
            null,
            null,
            scopeType,
            scopeValue);
    }

    private static SkuOrderRow Row(
        decimal currentPrice,
        decimal? originalPrice = null,
        int quantity = 1,
        Guid? productId = null,
        Guid? categoryId = null,
        IEnumerable<Guid>? categoryIds = null)
    {
        return new SkuOrderRow
        {
            SkuId = Guid.NewGuid(),
            ProductId = productId ?? Guid.NewGuid(),
            CategoryId = categoryId,
            CategoryIds = categoryIds?.ToList() ?? new List<Guid>(),
            OriginalPrice = originalPrice ?? currentPrice,
            CurrentPrice = currentPrice,
            Quantity = quantity
        };
    }

    [Fact]
    public void Evaluate_FixedCoupon_ComputesDiscountOnPromotionPrice()
    {
        var coupon = Template(150m, 30m, null, "fixed", "all", null);
        var memberCoupon = Issued(coupon.Id);
        // 促销后金额 100 + 120 = 220，原价更高，抵扣基数必须是促销后金额
        var items = new[]
        {
            Row(100m, originalPrice: 150m),
            Row(120m, originalPrice: 180m)
        };

        var result = MallCouponService.Evaluate(memberCoupon, coupon, items);

        Assert.True(result.IsAvailable);
        Assert.Equal(30m, result.DiscountAmount);
    }

    [Fact]
    public void Evaluate_ReturnsGapWhenThresholdNotMet()
    {
        var coupon = Template(250m, 30m, null, "fixed", "all", null);
        var memberCoupon = Issued(coupon.Id);

        var result = MallCouponService.Evaluate(memberCoupon, coupon, new[] { Row(100m), Row(120m) });

        Assert.False(result.IsAvailable);
        Assert.Equal("还差 ¥30.00 可使用", result.UnavailableReason);
        Assert.Equal(30m, result.GapAmount);
    }

    [Fact]
    public void Evaluate_UnavailableWhenCouponAlreadyUsed()
    {
        var coupon = Template(0m, 10m, null, "fixed", "all", null);
        var memberCoupon = Issued(coupon.Id);
        memberCoupon.Use("TEST-ORDER");

        var result = MallCouponService.Evaluate(memberCoupon, coupon, new[] { Row(100m) });

        Assert.False(result.IsAvailable);
        Assert.Equal("优惠券已使用", result.UnavailableReason);
    }

    [Fact]
    public void Evaluate_UnavailableWhenExpired()
    {
        var coupon = Template(0m, 10m, null, "fixed", "all", null);
        var memberCoupon = Issued(coupon.Id, DateTime.Now.AddDays(-1));

        var result = MallCouponService.Evaluate(memberCoupon, coupon, new[] { Row(100m) });

        Assert.False(result.IsAvailable);
        Assert.Equal("优惠券已过期", result.UnavailableReason);
    }

    [Fact]
    public void Evaluate_ProductScope_OnlyCountsMatchingProduct()
    {
        var targetProductId = Guid.NewGuid();
        var coupon = Template(110m, 20m, null, "fixed", "product", targetProductId.ToString());
        var memberCoupon = Issued(coupon.Id);
        var items = new[]
        {
            Row(100m, productId: targetProductId),
            Row(120m)
        };

        var result = MallCouponService.Evaluate(memberCoupon, coupon, items);

        Assert.False(result.IsAvailable);
        Assert.Equal("还差 ¥10.00 可使用", result.UnavailableReason);
        Assert.Equal(10m, result.GapAmount);
    }

    [Fact]
    public void Evaluate_CategoryScope_IncludesAncestorCategories()
    {
        var parentCategoryId = Guid.NewGuid();
        var childCategoryId = Guid.NewGuid();
        var coupon = Template(100m, 20m, null, "fixed", "category", parentCategoryId.ToString());
        var memberCoupon = Issued(coupon.Id);
        var items = new[]
        {
            Row(100m, categoryId: childCategoryId, categoryIds: new[] { childCategoryId, parentCategoryId }),
            Row(120m)
        };

        var result = MallCouponService.Evaluate(memberCoupon, coupon, items);

        Assert.True(result.IsAvailable);
        Assert.Equal(20m, result.DiscountAmount);
    }

    [Fact]
    public void Evaluate_UnavailableWhenNoScopedItems()
    {
        var targetProductId = Guid.NewGuid();
        var coupon = Template(0m, 20m, null, "fixed", "product", targetProductId.ToString());
        var memberCoupon = Issued(coupon.Id);

        var result = MallCouponService.Evaluate(memberCoupon, coupon, new[] { Row(120m) });

        Assert.False(result.IsAvailable);
        Assert.Equal("订单中没有适用该券的商品", result.UnavailableReason);
    }

    [Fact]
    public void Evaluate_PercentCoupon_RespectsMaxDiscount()
    {
        var coupon = Template(0m, 0.8m, 50m, "percent", "all", null);
        var memberCoupon = Issued(coupon.Id);

        var result = MallCouponService.Evaluate(memberCoupon, coupon, new[] { Row(100m), Row(120m) });

        Assert.True(result.IsAvailable);
        Assert.Equal(50m, result.DiscountAmount);
    }
}
