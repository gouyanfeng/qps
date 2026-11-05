using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 促销类型枚举，直接映射到数据库列 MallPromotion.PromotionType。
/// </summary>
public enum MallPromotionType
{
    DiscountAmount = 0,   // 满减
    DiscountRate = 1,      // 折扣
    Gift = 2               // 买赠
}

public class MallPromotion : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>促销类型（0 满减 / 1 折扣 / 2 买赠），独立列，用于查询筛选。</summary>
    public int PromotionType { get; private set; }

    /// <summary>订单门槛金额（单位：元），独立列。按范围内商品金额累计 ≥ 此值时促销生效。</summary>
    public decimal ThresholdAmount { get; private set; }

    public string ScopeType { get; private set; } = "Global";

    public int MaxUsagePerUser { get; private set; }

    public int MaxUsagePerOrder { get; private set; }

    /// <summary>可选的促销自身时间窗口（为空则长期有效）。</summary>
    public DateTime? StartDate { get; private set; }

    public DateTime? EndDate { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>动作类型：DiscountAmount / DiscountRate / Gift。</summary>
    public string ActionType { get; private set; } = string.Empty;

    /// <summary>动作参数 JSON：{amount} / {rate} / {skuId,quantity}。</summary>
    public string ActionConfigJson { get; private set; } = "{}";

    /// <summary>促销标签（可选，关联 MallTags），参与该促销的商品展示时带上此标签。</summary>
    public Guid? PromotionTagId { get; private set; }

    private MallPromotion() { }

    private MallPromotion(
        string name,
        string description,
        MallPromotionType promotionType,
        decimal thresholdAmount,
        string actionType,
        string actionConfigJson,
        string scopeType,
        Guid? promotionTagId,
        int maxUsagePerUser,
        int maxUsagePerOrder,
        DateTime? startDate,
        DateTime? endDate,
        bool isActive)
    {
        Name = name;
        Description = description;
        PromotionType = (int)promotionType;
        ThresholdAmount = thresholdAmount;
        ActionType = actionType;
        ActionConfigJson = actionConfigJson;
        ScopeType = scopeType;
        PromotionTagId = promotionTagId;
        MaxUsagePerUser = maxUsagePerUser;
        MaxUsagePerOrder = maxUsagePerOrder;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }

    public static MallPromotion Create(
        string name,
        string description,
        MallPromotionType promotionType,
        decimal thresholdAmount,
        string actionType,
        string actionConfigJson,
        string scopeType,
        Guid? promotionTagId,
        int maxUsagePerUser,
        int maxUsagePerOrder,
        DateTime? startDate,
        DateTime? endDate,
        bool isActive)
    {
        return new MallPromotion(
            name, description, promotionType, thresholdAmount,
            actionType, actionConfigJson, scopeType, promotionTagId,
            maxUsagePerUser, maxUsagePerOrder, startDate, endDate, isActive);
    }

    public void Update(
        string name,
        string description,
        MallPromotionType promotionType,
        decimal thresholdAmount,
        string actionType,
        string actionConfigJson,
        string scopeType,
        Guid? promotionTagId,
        int maxUsagePerUser,
        int maxUsagePerOrder,
        DateTime? startDate,
        DateTime? endDate)
    {
        Name = name;
        Description = description;
        PromotionType = (int)promotionType;
        ThresholdAmount = thresholdAmount;
        ActionType = actionType;
        ActionConfigJson = actionConfigJson;
        ScopeType = scopeType;
        PromotionTagId = promotionTagId;
        MaxUsagePerUser = maxUsagePerUser;
        MaxUsagePerOrder = maxUsagePerOrder;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void ChangeStatus(bool isActive) => IsActive = isActive;
}