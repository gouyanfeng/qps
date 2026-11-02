using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public class MallPromotion : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string TriggerType { get; private set; } = string.Empty;

    public string TriggerConfigJson { get; private set; } = "{}";

    public string ActionType { get; private set; } = string.Empty;

    public string ActionConfigJson { get; private set; } = "{}";

    public string ScopeType { get; private set; } = "Global";

    public int MaxUsagePerUser { get; private set; }

    public int MaxUsagePerOrder { get; private set; }

    public DateTime? StartDate { get; private set; }

    public DateTime? EndDate { get; private set; }

    public bool IsActive { get; private set; }

    private MallPromotion()
    {
    }

    private MallPromotion(
        string name,
        string description,
        string triggerType,
        string triggerConfigJson,
        string actionType,
        string actionConfigJson,
        string scopeType,
        int maxUsagePerUser,
        int maxUsagePerOrder,
        DateTime? startDate,
        DateTime? endDate,
        bool isActive)
    {
        Name = name;
        Description = description;
        TriggerType = triggerType;
        TriggerConfigJson = triggerConfigJson;
        ActionType = actionType;
        ActionConfigJson = actionConfigJson;
        ScopeType = scopeType;
        MaxUsagePerUser = maxUsagePerUser;
        MaxUsagePerOrder = maxUsagePerOrder;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }

    public static MallPromotion Create(
        string name,
        string description,
        string triggerType,
        string triggerConfigJson,
        string actionType,
        string actionConfigJson,
        string scopeType,
        int maxUsagePerUser,
        int maxUsagePerOrder,
        DateTime? startDate,
        DateTime? endDate,
        bool isActive)
    {
        return new MallPromotion(
            name,
            description,
            triggerType,
            triggerConfigJson,
            actionType,
            actionConfigJson,
            scopeType,
            maxUsagePerUser,
            maxUsagePerOrder,
            startDate,
            endDate,
            isActive);
    }

    public void Update(
        string name,
        string description,
        string triggerType,
        string triggerConfigJson,
        string actionType,
        string actionConfigJson,
        string scopeType,
        int maxUsagePerUser,
        int maxUsagePerOrder,
        DateTime? startDate,
        DateTime? endDate)
    {
        Name = name;
        Description = description;
        TriggerType = triggerType;
        TriggerConfigJson = triggerConfigJson;
        ActionType = actionType;
        ActionConfigJson = actionConfigJson;
        ScopeType = scopeType;
        MaxUsagePerUser = maxUsagePerUser;
        MaxUsagePerOrder = maxUsagePerOrder;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void ChangeStatus(bool isActive) => IsActive = isActive;

    public bool IsWithinTimeWindow(DateTime now)
    {
        if (StartDate.HasValue && now < StartDate.Value) return false;
        if (EndDate.HasValue && now > EndDate.Value) return false;
        return true;
    }
}