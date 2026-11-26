using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Application.Features.MallFreightRules;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 结算口径定价：在促销试算结果之上叠加优惠券与运费，产出统一的 PriceBreakdown。
/// 购物车页（无地址无券）与结算页（有地址可选券）共用这一处，避免两套价格逻辑漂移。
/// </summary>
public class MallCheckoutPricingService
{
    private readonly IMallDbContext _dbContext;
    private readonly MallCouponService _couponService;

    public MallCheckoutPricingService(IMallDbContext dbContext, MallCouponService couponService)
    {
        _dbContext = dbContext;
        _couponService = couponService;
    }

    /// <summary>
    /// 按促销试算结果计算券抵扣与运费，得到最终应付拆分。
    /// addressId 为空时运费为 0（购物车阶段）；memberCouponId 为空时不抵扣。
    /// </summary>
    public async Task<CheckoutPricing> CalculateAsync(
        Guid memberId,
        MallPromotionPreview preview,
        IReadOnlyCollection<MallPromotionPreviewItem> items,
        Guid? addressId,
        Guid? memberCouponId,
        CancellationToken cancellationToken)
    {
        var couponDiscount = 0m;
        string? couponName = null;
        decimal? couponGapAmount = null;
        string? couponHint = null;
        if (memberCouponId.HasValue && memberCouponId.Value != Guid.Empty)
        {
            // 预览不抛异常：券不可用时返回原因与差额，折扣按 0 计算，结算页据此提示
            var evaluation = await _couponService.EvaluateForPreviewAsync(
                memberId, memberCouponId.Value, preview.PromotionItems, cancellationToken);

            if (evaluation.IsAvailable)
            {
                couponDiscount = evaluation.DiscountAmount;
                couponName = evaluation.CouponName;
            }
            else
            {
                couponGapAmount = evaluation.GapAmount;
                couponHint = evaluation.UnavailableReason;
            }
        }

        var freightAmount = addressId.HasValue && addressId.Value != Guid.Empty
            ? await CalculateFreightAsync(memberId, addressId.Value, items, preview, cancellationToken)
            : 0m;

        var breakdown = new PriceBreakdown(
            preview.GoodsAmount, preview.DiscountAmount, couponDiscount, freightAmount);

        return new CheckoutPricing(
            couponDiscount, couponName, couponGapAmount, couponHint, freightAmount, breakdown);
    }

    /// <summary>预览运费与下单共用同一计算入口；无地址或免邮时为 0，不报错。</summary>
    private async Task<decimal> CalculateFreightAsync(
        Guid memberId,
        Guid addressId,
        IReadOnlyCollection<MallPromotionPreviewItem> items,
        MallPromotionPreview preview,
        CancellationToken cancellationToken)
    {
        var address = await _dbContext.MallAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == addressId && item.UserId == memberId,
                cancellationToken);

        // 地址必须属于当前会员，不能只校验非空，避免越权读取他人地址
        if (address is null)
        {
            throw new BusinessException(400, "收货地址不存在或不属于当前会员");
        }

        var addressInfo = new FreightAddressInfo
        {
            ProvinceName = address.ProvinceName,
            CityName = address.CityName
        };

        var quantityBySku = items
            .GroupBy(item => item.SkuId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        // 运费按促销后的商品行金额计算（不含券），与下单保持一致
        var priceBySku = preview.PromotionItems
            .GroupBy(item => item.SkuId)
            .ToDictionary(group => group.Key, group => group.First().CurrentPrice);

        var skuIds = quantityBySku.Keys.ToList();

        var skuRows = await (
                from sku in _dbContext.MallSkus.AsNoTracking()
                join product in _dbContext.MallProducts.AsNoTracking() on sku.ProductId equals product.Id
                where skuIds.Contains(sku.Id)
                select new { Sku = sku, Product = product })
            .ToListAsync(cancellationToken);

        var freightItems = skuRows
            .Select(row =>
            {
                var quantity = quantityBySku[row.Sku.Id];
                var unitPrice = priceBySku.TryGetValue(row.Sku.Id, out var price)
                    ? price
                    : row.Sku.Price;

                return new FreightItemRow
                {
                    SkuId = row.Sku.Id,
                    ProductId = row.Product.Id,
                    FreightTemplateId = row.Product.FreightTemplateId,
                    Quantity = quantity,
                    WeightGram = row.Sku.WeightGram,
                    VolumeCm3 = row.Sku.VolumeCm3,
                    LineAmount = unitPrice * quantity
                };
            })
            .ToList();

        return await MallFreightCalculator.CalculateAsync(
            _dbContext,
            addressInfo,
            freightItems,
            preview.IsFreightFree,
            cancellationToken);
    }
}

/// <summary>结算定价结果：券、运费与统一应付拆分。</summary>
public sealed record CheckoutPricing(
    decimal CouponDiscountAmount,
    string? CouponName,
    decimal? CouponGapAmount,
    string? CouponHint,
    decimal FreightAmount,
    PriceBreakdown Breakdown);
