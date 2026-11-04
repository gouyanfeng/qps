using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallPromotions;

/// <summary>
/// 促销有效性统一口径：启用、未删除，挂载在至少一个进行中的活动上，
/// 且在促销自身时间窗口内（未配置时间则不限，即与活动时间取交集）。
/// </summary>
public static class MallPromotionEffectiveQuery
{
    public static IQueryable<MallPromotion> WhereEffective(
        this IQueryable<MallPromotion> query,
        IMallDbContext db,
        DateTime now)
    {
        return query.Where(promotion =>
            promotion.IsActive &&
            !promotion.IsDeleted &&
            (promotion.StartDate == null || promotion.StartDate <= now) &&
            (promotion.EndDate == null || promotion.EndDate >= now) &&
            db.MallActivityPromotions.Any(link =>
                !link.IsDeleted &&
                link.PromotionId == promotion.Id &&
                db.MallActivities.Any(activity =>
                    activity.Id == link.ActivityId &&
                    activity.IsActive &&
                    !activity.IsDeleted &&
                    (activity.StartDate == null || activity.StartDate <= now) &&
                    (activity.EndDate == null || activity.EndDate >= now))));
    }
}
