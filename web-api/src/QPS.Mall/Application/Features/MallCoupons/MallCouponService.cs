using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCoupons;

/// <summary>
/// 优惠券可用性判定与抵扣计算：确认页选券、订单预览、下单校验共用同一口径。
/// 计算顺序固定为「商品金额 → 促销 → 券抵扣 → 应付」，因此门槛与抵扣基数均为促销后的商品金额。
/// </summary>
public class MallCouponService
{
    private readonly IMallDbContext _dbContext;

    public MallCouponService(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>按会员券 ID 校验归属、状态、有效期、门槛与适用范围，并计算抵扣金额。</summary>
    public async Task<CouponEvaluation> EvaluateAsync(
        Guid memberId,
        Guid memberCouponId,
        IReadOnlyCollection<SkuOrderRow> promotionItems,
        CancellationToken cancellationToken)
    {
        var memberCoupon = await _dbContext.MallMemberCoupons
            .AsNoTracking()
            .FirstOrDefaultAsync(
                coupon => coupon.Id == memberCouponId && coupon.MemberId == memberId,
                cancellationToken);

        if (memberCoupon is null)
        {
            throw new BusinessException(400, "优惠券不存在或不属于当前账号");
        }

        var coupon = await _dbContext.MallCoupons
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == memberCoupon.CouponId, cancellationToken);

        if (coupon is null)
        {
            throw new BusinessException(400, "优惠券不存在");
        }

        var evaluation = Evaluate(memberCoupon, coupon, promotionItems);
        if (!evaluation.IsAvailable)
        {
            throw new BusinessException(400, evaluation.UnavailableReason ?? "优惠券不可用");
        }

        return evaluation;
    }

    /// <summary>
    /// 预览用判定：券不存在或不可用时返回不可用结果而不抛异常，便于结算页展示“还差 X 元”等提示。
    /// </summary>
    public async Task<CouponEvaluation> EvaluateForPreviewAsync(
        Guid memberId,
        Guid memberCouponId,
        IReadOnlyCollection<SkuOrderRow> promotionItems,
        CancellationToken cancellationToken)
    {
        var memberCoupon = await _dbContext.MallMemberCoupons
            .AsNoTracking()
            .FirstOrDefaultAsync(
                coupon => coupon.Id == memberCouponId && coupon.MemberId == memberId,
                cancellationToken);

        if (memberCoupon is null)
        {
            return CouponEvaluation.Unavailable("优惠券不存在或不属于当前账号");
        }

        var coupon = await _dbContext.MallCoupons
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == memberCoupon.CouponId, cancellationToken);

        if (coupon is null)
        {
            return CouponEvaluation.Unavailable("优惠券不存在");
        }

        return Evaluate(memberCoupon, coupon, promotionItems);
    }

    /// <summary>同步判定，供确认页批量列出可用券时复用。</summary>
    public static CouponEvaluation Evaluate(
        MallMemberCoupon memberCoupon,
        MallCoupon coupon,
        IReadOnlyCollection<SkuOrderRow> promotionItems)
    {
        var now = DateTime.Now;

        if (!coupon.IsActive)
        {
            return CouponEvaluation.Unavailable("优惠券已停用");
        }

        if (coupon.StartAt.HasValue && now < coupon.StartAt.Value)
        {
            return CouponEvaluation.Unavailable("优惠券尚未开始使用");
        }

        if (coupon.EndAt.HasValue && now > coupon.EndAt.Value)
        {
            return CouponEvaluation.Unavailable("优惠券已过期");
        }

        if (memberCoupon.Status != "claimed")
        {
            return CouponEvaluation.Unavailable(
                memberCoupon.Status == "used" ? "优惠券已使用" : "优惠券不可用");
        }

        if (memberCoupon.ExpireAt < now)
        {
            return CouponEvaluation.Unavailable("优惠券已过期");
        }

        var scopeIds = ParseScopeIds(coupon.ScopeValue);
        decimal scopeAmount;
        switch (coupon.ScopeType.Trim().ToLowerInvariant())
        {
            case "product":
                scopeAmount = promotionItems
                    .Where(item => scopeIds.Contains(item.ProductId))
                    .Sum(item => item.CurrentPrice * item.Quantity);
                break;
            case "category":
                scopeAmount = promotionItems
                    .Where(item => (item.CategoryId.HasValue && scopeIds.Contains(item.CategoryId.Value))
                        || item.CategoryIds.Any(scopeIds.Contains))
                    .Sum(item => item.CurrentPrice * item.Quantity);
                break;
            case "all":
            case "":
                scopeAmount = promotionItems.Sum(item => item.CurrentPrice * item.Quantity);
                break;
            default:
                return CouponEvaluation.Unavailable("优惠券适用范围配置异常");
        }

        if (scopeAmount <= 0)
        {
            return CouponEvaluation.Unavailable("订单中没有适用该券的商品");
        }

        if (scopeAmount < coupon.ThresholdAmount)
        {
            var gap = coupon.ThresholdAmount - scopeAmount;
            return CouponEvaluation.Unavailable($"还差 ¥{gap:0.00} 可使用", Math.Round(gap, 2));
        }

        var discount = Math.Clamp(coupon.CalculateDiscount(scopeAmount), 0, scopeAmount);
        if (discount <= 0)
        {
            return CouponEvaluation.Unavailable("优惠券没有可抵扣金额");
        }

        return CouponEvaluation.Available(discount, coupon.Id, coupon.Name);
    }

    /// <summary>
    /// 下单事务内核销：条件更新保证并发下同一张券只会被使用一次，受影响行数为 0 时抛异常回滚。
    /// </summary>
    public static async Task RedeemAsync(
        IMallDbContext dbContext,
        Guid memberCouponId,
        Guid memberId,
        string orderNo,
        CancellationToken cancellationToken)
    {
        var affected = await dbContext.MallMemberCoupons
            .Where(coupon => coupon.Id == memberCouponId
                && coupon.MemberId == memberId
                && coupon.Status == "claimed"
                && coupon.ExpireAt > DateTime.Now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(coupon => coupon.Status, "used")
                    .SetProperty(coupon => coupon.UsedAt, DateTime.Now)
                    .SetProperty(coupon => coupon.UsedOrderNo, orderNo),
                cancellationToken);

        if (affected == 0)
        {
            throw new BusinessException(400, "优惠券已被使用或已过期，请重新选择");
        }
    }

    /// <summary>取消订单 / 退款成功后回补券：条件更新仅回补该订单占用的券。</summary>
    public static async Task ReleaseByOrderNoAsync(
        IMallDbContext dbContext,
        string orderNo,
        CancellationToken cancellationToken)
    {
        await dbContext.MallMemberCoupons
            .Where(coupon => coupon.UsedOrderNo == orderNo && coupon.Status == "used")
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(coupon => coupon.Status, "claimed")
                    .SetProperty(coupon => coupon.UsedAt, (DateTime?)null)
                    .SetProperty(coupon => coupon.UsedOrderNo, (string?)null),
                cancellationToken);
    }

    private static HashSet<Guid> ParseScopeIds(string? scopeValue)
    {
        if (string.IsNullOrWhiteSpace(scopeValue))
        {
            return new HashSet<Guid>();
        }

        return scopeValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
    }
}

/// <summary>券判定结果：可用时返回抵扣金额与券信息（供订单快照），不可用时返回原因。</summary>
public sealed class CouponEvaluation
{
    private CouponEvaluation(
        bool isAvailable,
        decimal discountAmount,
        string? unavailableReason,
        decimal? gapAmount = null,
        Guid? couponId = null,
        string couponName = "")
    {
        IsAvailable = isAvailable;
        DiscountAmount = discountAmount;
        UnavailableReason = unavailableReason;
        GapAmount = gapAmount;
        CouponId = couponId;
        CouponName = couponName;
    }

    public bool IsAvailable { get; }

    public decimal DiscountAmount { get; }

    public string? UnavailableReason { get; }

    /// <summary>因差额未达门槛时还差的金额，仅门槛不足场景有值。</summary>
    public decimal? GapAmount { get; }

    /// <summary>券模板 ID，下单时写入订单快照；不可用时为空。</summary>
    public Guid? CouponId { get; }

    /// <summary>券名称，下单时写入订单快照；不可用时为空。</summary>
    public string CouponName { get; }

    public static CouponEvaluation Available(decimal discountAmount, Guid? couponId = null, string couponName = "") =>
        new(true, discountAmount, null, null, couponId, couponName);

    public static CouponEvaluation Unavailable(string reason, decimal? gapAmount = null) =>
        new(false, 0, reason, gapAmount);
}
