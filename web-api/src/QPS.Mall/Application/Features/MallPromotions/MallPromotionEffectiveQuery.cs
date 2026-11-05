using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallPromotions;

/// <summary>
/// 促销有效性统一口径：启用、未删除，且在促销自身时间窗口内（未配置时间则不限）。
/// </summary>
public static class MallPromotionEffectiveQuery
{
    public static IQueryable<MallPromotion> WhereEffective(
        this IQueryable<MallPromotion> query,
        DateTime now)
    {
        return query.Where(promotion =>
            promotion.IsActive &&
            !promotion.IsDeleted &&
            (promotion.StartDate == null || promotion.StartDate <= now) &&
            (promotion.EndDate == null || promotion.EndDate >= now));
    }
}