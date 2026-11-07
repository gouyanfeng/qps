using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Application.Features.MallFreightRules;
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
    private readonly MallCouponService _couponService;
    private readonly IMallDbContext _dbContext;

    public PreviewMallOrderHandler(
        MallPromotionPreviewService promotionPreview,
        MallCouponService couponService,
        IMallDbContext dbContext)
    {
        _promotionPreview = promotionPreview;
        _couponService = couponService;
        _dbContext = dbContext;
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

        var couponDiscount = 0m;
        string? couponName = null;
        decimal? couponGapAmount = null;
        string? couponHint = null;
        if (request.Request.MemberCouponId.HasValue)
        {
            // 预览不抛异常：券不可用时返回原因与差额，折扣按 0 计算，结算页据此提示
            var evaluation = await _couponService.EvaluateForPreviewAsync(
                request.MemberId,
                request.Request.MemberCouponId.Value,
                preview.PromotionItems,
                cancellationToken);

            if (evaluation.IsAvailable)
            {
                couponDiscount = evaluation.DiscountAmount;
            }
            else
            {
                couponGapAmount = evaluation.GapAmount;
                couponHint = evaluation.UnavailableReason;
            }

            couponName = await _dbContext.MallMemberCoupons
                .AsNoTracking()
                .Where(coupon => coupon.Id == request.Request.MemberCouponId.Value)
                .Join(
                    _dbContext.MallCoupons.AsNoTracking(),
                    coupon => coupon.CouponId,
                    template => template.Id,
                    (coupon, template) => template.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var freightAmount = await CalculateFreightAsync(request, preview, cancellationToken);

        // 商品部分封底为 0，运费在封底后再加，保证与下单口径一致
        var discountedGoodsAmount = Math.Max(
            0,
            preview.GoodsAmount - preview.DiscountAmount - couponDiscount);

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
            CouponDiscountAmount = couponDiscount,
            CouponName = couponName,
            CouponGapAmount = couponGapAmount,
            CouponHint = couponHint,
            FreightAmount = freightAmount,
            PayableAmount = discountedGoodsAmount + freightAmount,
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

    /// <summary>预览运费与下单共用同一计算入口；无地址或免邮时为 0，不报错。</summary>
    private async Task<decimal> CalculateFreightAsync(
        PreviewMallOrderQuery request,
        MallPromotionPreview preview,
        CancellationToken cancellationToken)
    {
        var address = await _dbContext.MallAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == request.Request.AddressId && item.UserId == request.MemberId,
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

        var quantityBySku = request.Request.Items
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
