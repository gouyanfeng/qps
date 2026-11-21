using QPS.Shared.Common;

namespace QPS.Crm.Domain.Entities;

/// <summary>
/// 撮合机会子表：发起方与候选统一存储，用 EntityType + EntityId 指向原明细。
/// 业务字段（品类/数量/规格/价格等）不存副本，展示时实时关联原明细。
/// </summary>
public class CrmMatchOpportunityItem : BaseEntity
{
    public const string EntityTypeSupply = "supply";
    public const string EntityTypeDemand = "demand";

    public const string Initiator = "initiator";
    public const string Candidate = "candidate";

    /// <summary>统一英文状态，供应侧与需求侧不再区分中英文。</summary>
    public const string Pending = "pending";
    public const string Kept = "kept";
    public const string Selected = "selected";
    public const string Eliminated = "eliminated";

    public Guid OpportunityId { get; private set; }
    /// <summary>指向哪张原明细表：supply=CrmHerbBaseSupplies，demand=CrmVendorDemandItems。</summary>
    public string EntityType { get; private set; } = EntityTypeSupply;
    /// <summary>原明细 ID。</summary>
    public Guid EntityId { get; private set; }
    /// <summary>initiator=发起方，candidate=候选方。</summary>
    public string Role { get; private set; } = Candidate;
    public string Status { get; private set; } = Pending;
    public string EliminationReason { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    private CrmMatchOpportunityItem()
    {
    }

    public static CrmMatchOpportunityItem Create(
        Guid opportunityId,
        string entityType,
        Guid entityId,
        string role,
        int sortOrder)
    {
        if (opportunityId == Guid.Empty) throw new InvalidOperationException("撮合机会不能为空");
        if (entityId == Guid.Empty) throw new InvalidOperationException("关联明细不能为空");
        if (entityType != EntityTypeSupply && entityType != EntityTypeDemand)
        {
            throw new InvalidOperationException("关联明细类型无效");
        }

        return new CrmMatchOpportunityItem
        {
            OpportunityId = opportunityId,
            EntityType = entityType,
            EntityId = entityId,
            Role = role == Initiator ? Initiator : Candidate,
            SortOrder = sortOrder,
            Status = Pending
        };
    }

    public void ResetToPending()
    {
        Status = Pending;
        EliminationReason = string.Empty;
    }

    public void Keep()
    {
        Status = Kept;
        EliminationReason = string.Empty;
    }

    public void Select()
    {
        Status = Selected;
        EliminationReason = string.Empty;
    }

    public void Eliminate(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("淘汰必须填写原因");
        }

        Status = Eliminated;
        EliminationReason = reason.Trim();
    }

    /// <summary>把界面提交的中文状态归一为统一的英文枚举。</summary>
    public static string NormalizeStatus(string? status) => status?.Trim() switch
    {
        "待评估" => Pending,
        "保留" => Kept,
        "中选" => Selected,
        "淘汰" => Eliminated,
        null or "" => string.Empty,
        var other => other
    };
}
