using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public class MallPromotionUsage : BaseEntity
{
    public Guid PromotionId { get; private set; }

    public Guid UserId { get; private set; }

    public int UsageCount { get; private set; }

    public DateTime? LastUsedAt { get; private set; }

    private MallPromotionUsage()
    {
    }

    private MallPromotionUsage(Guid promotionId, Guid userId)
    {
        PromotionId = promotionId;
        UserId = userId;
        UsageCount = 0;
    }

    public static MallPromotionUsage Create(Guid promotionId, Guid userId)
        => new(promotionId, userId);

    public void Increment(DateTime now)
    {
        UsageCount++;
        LastUsedAt = now;
    }

    public void Decrement()
    {
        if (UsageCount > 0) UsageCount--;
    }
}