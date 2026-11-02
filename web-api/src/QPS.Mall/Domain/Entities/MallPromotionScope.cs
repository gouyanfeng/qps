using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public class MallPromotionScope : BaseEntity
{
    public Guid PromotionId { get; private set; }

    public string ScopeTargetType { get; private set; } = string.Empty;

    public Guid ScopeTargetId { get; private set; }

    private MallPromotionScope()
    {
    }

    private MallPromotionScope(Guid promotionId, string scopeTargetType, Guid scopeTargetId)
    {
        PromotionId = promotionId;
        ScopeTargetType = scopeTargetType;
        ScopeTargetId = scopeTargetId;
    }

    public static MallPromotionScope Create(Guid promotionId, string scopeTargetType, Guid scopeTargetId)
        => new(promotionId, scopeTargetType, scopeTargetId);
}