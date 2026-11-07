using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Contracts.DTOs.MallCoupons;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallMemberCoupons;

/// <summary>确认页选券：按当前结算商品返回会员券列表，并标注可用性与不可用原因。</summary>
public class GetMallCheckoutCouponsQuery : IRequest<List<MallCheckoutCouponDto>>
{
    public Guid MemberId { get; set; }

    public MallCheckoutCouponQueryRequest Request { get; set; } = new();
}

public class GetMallCheckoutCouponsHandler
    : IRequestHandler<GetMallCheckoutCouponsQuery, List<MallCheckoutCouponDto>>
{
    private readonly IMallDbContext _dbContext;
    private readonly MallPromotionPreviewService _promotionPreview;

    public GetMallCheckoutCouponsHandler(
        IMallDbContext dbContext,
        MallPromotionPreviewService promotionPreview)
    {
        _dbContext = dbContext;
        _promotionPreview = promotionPreview;
    }

    public async Task<List<MallCheckoutCouponDto>> Handle(
        GetMallCheckoutCouponsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Request.Items is null || query.Request.Items.Count == 0)
        {
            throw new BusinessException(400, "请选择要结算的商品");
        }

        // 与预览/下单同口径：券门槛与抵扣基数都按促销后的商品金额计算
        var preview = await _promotionPreview.PreviewAsync(
            query.MemberId,
            query.Request.Items
                .Select(item => new MallPromotionPreviewItem(item.SkuId, item.Quantity))
                .ToList(),
            cancellationToken);

        var rows = await (
                from memberCoupon in _dbContext.MallMemberCoupons.AsNoTracking()
                join coupon in _dbContext.MallCoupons.AsNoTracking()
                    on memberCoupon.CouponId equals coupon.Id
                where memberCoupon.MemberId == query.MemberId
                select new { MemberCoupon = memberCoupon, Coupon = coupon })
            .OrderByDescending(row => row.MemberCoupon.ClaimedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(row =>
        {
            var evaluation = MallCouponService.Evaluate(
                row.MemberCoupon,
                row.Coupon,
                preview.PromotionItems);

            return new MallCheckoutCouponDto
            {
                Id = row.MemberCoupon.Id,
                CouponId = row.Coupon.Id,
                Name = row.Coupon.Name,
                CouponType = row.Coupon.CouponType,
                ThresholdAmount = row.Coupon.ThresholdAmount,
                FaceValue = row.Coupon.DiscountAmount,
                MaxDiscount = row.Coupon.MaxDiscount,
                ScopeType = row.Coupon.ScopeType,
                ExpireAt = row.MemberCoupon.ExpireAt,
                IsAvailable = evaluation.IsAvailable,
                UnavailableReason = evaluation.UnavailableReason,
                DiscountAmount = evaluation.DiscountAmount
            };
        }).ToList();
    }
}
