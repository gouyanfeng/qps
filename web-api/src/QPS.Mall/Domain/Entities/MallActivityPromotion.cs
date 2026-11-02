using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 活动与促销的多对多关联记录。
/// </summary>
public class MallActivityPromotion : BaseEntity
{
    public Guid ActivityId { get; private set; }

    public Guid PromotionId { get; private set; }

    private MallActivityPromotion()
    {
    }

    private MallActivityPromotion(Guid activityId, Guid promotionId)
    {
        ActivityId = activityId;
        PromotionId = promotionId;
    }

    public static MallActivityPromotion Create(Guid activityId, Guid promotionId)
        => new(activityId, promotionId);
}
