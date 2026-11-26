using MediatR;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>订单确认页价格预览：按当前商品、数量与可选优惠券试算应付金额。</summary>
public class PreviewMallOrderQuery : IRequest<MallOrderPreviewDto>
{
    public Guid MemberId { get; set; }

    public PreviewMallOrderRequest Request { get; set; } = new();
}

public class PreviewMallOrderHandler : IRequestHandler<PreviewMallOrderQuery, MallOrderPreviewDto>
{
    private readonly MallPromotionPreviewService _promotionPreview;
    private readonly MallCheckoutPricingService _checkoutPricing;

    /// <summary>单一构造函数：内部装配统一定价服务，避免多构造导致 DI 歧义。</summary>
    public PreviewMallOrderHandler(
        MallPromotionPreviewService promotionPreview,
        MallCouponService couponService,
        IMallDbContext dbContext)
    {
        _promotionPreview = promotionPreview;
        _checkoutPricing = new MallCheckoutPricingService(dbContext, couponService);
    }

    public async Task<MallOrderPreviewDto> Handle(
        PreviewMallOrderQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Request.Items is null || request.Request.Items.Count == 0)
        {
            throw new BusinessException(400, "请选择要结算的商品");
        }

        // 预览必须带收货地址：运费依赖地址，无地址时不允许按 0 运费试算
        if (!request.Request.AddressId.HasValue || request.Request.AddressId.Value == Guid.Empty)
        {
            throw new BusinessException(400, "请选择收货地址");
        }

        var items = request.Request.Items
            .Select(item => new MallPromotionPreviewItem(item.SkuId, item.Quantity))
            .ToList();

        var preview = await _promotionPreview.PreviewAsync(
            request.MemberId,
            items,
            cancellationToken);

        var pricing = await _checkoutPricing.CalculateAsync(
            request.MemberId,
            preview,
            items,
            request.Request.AddressId,
            request.Request.MemberCouponId,
            cancellationToken);

        // 赠品在结算阶段才由促销引擎算出，提交前先透出清单
        var gifts = await _promotionPreview.ResolveGiftItemsAsync(
            preview.GiftSkus,
            cancellationToken);

        return new MallOrderPreviewDto
        {
            GoodsAmount = preview.GoodsAmount,
            PromotionDiscountAmount = preview.DiscountAmount,
            RateDiscountAmount = preview.RateDiscountAmount,
            AmountDiscountAmount = preview.AmountDiscountAmount,
            PromotionDetails = preview.Lines
                .Select(line => new MallOrderPromotionDto
                {
                    PromotionId = line.PromotionId,
                    Name = line.Name,
                    DiscountAmount = line.DiscountAmount
                })
                .ToList(),
            CouponDiscountAmount = pricing.CouponDiscountAmount,
            CouponName = pricing.CouponName,
            CouponGapAmount = pricing.CouponGapAmount,
            CouponHint = pricing.CouponHint,
            FreightAmount = pricing.FreightAmount,
            PayableAmount = pricing.Breakdown.PayableAmount,
            Gifts = gifts.Select(gift => new MallPromotionGiftDto
            {
                SkuId = gift.SkuId,
                ProductId = gift.ProductId,
                ProductName = gift.ProductName,
                ProductImage = gift.ProductImage,
                SpecName = gift.SpecName,
                Quantity = gift.Quantity,
                PromotionId = gift.PromotionId,
                PromotionName = gift.PromotionName
            }).ToList()
        };
    }
}
