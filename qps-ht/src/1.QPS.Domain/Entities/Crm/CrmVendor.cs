using QPS.Domain.Common;

namespace QPS.Domain.Entities.Crm;

/// <summary>
/// CRM厂商，承接药企求购线索，一家药企一行。
/// </summary>
public class CrmVendor : BaseEntity
{
    /// <summary>
    /// 厂商名称，展示用。
    /// </summary>
    public string VendorName { get; private set; } = string.Empty;

    /// <summary>
    /// 标准化厂商名称，用于去重。
    /// </summary>
    public string NormalizedVendorName { get; private set; } = string.Empty;

    /// <summary>
    /// 优先级：High、Medium、Low。
    /// </summary>
    public string PriorityLevel { get; private set; } = string.Empty;

    /// <summary>
    /// 最近采购时间。
    /// </summary>
    public DateTime? LatestPurchaseTime { get; private set; }

    /// <summary>
    /// 最近采购计划名称。
    /// </summary>
    public string LatestPurchasePlanName { get; private set; } = string.Empty;

    /// <summary>
    /// 备注。
    /// </summary>
    public string Remark { get; private set; } = string.Empty;

    /// <summary>
    /// 负责人用户ID。
    /// </summary>
    public Guid? OwnerUserId { get; private set; }

    public DateTime? LastFollowAt { get; private set; }

    public string LastFollowResult { get; private set; } = string.Empty;

    public DateTime? NextFollowAt { get; private set; }

    public virtual ICollection<CrmVendorPurchasePlan> PurchasePlans { get; private set; } = new List<CrmVendorPurchasePlan>();

    private CrmVendor()
    {
    }

    private CrmVendor(
        string vendorName,
        string normalizedVendorName,
        string priorityLevel,
        string remark,
        Guid? ownerUserId)
    {
        VendorName = vendorName;
        NormalizedVendorName = normalizedVendorName;
        PriorityLevel = priorityLevel;
        Remark = remark;
        OwnerUserId = ownerUserId;
    }

    public static CrmVendor Create(
        string vendorName,
        string normalizedVendorName,
        string priorityLevel,
        string remark,
        Guid? ownerUserId = null)
    {
        return new CrmVendor(vendorName, normalizedVendorName, priorityLevel, remark, ownerUserId);
    }

    public void Update(
        string vendorName,
        string normalizedVendorName,
        string priorityLevel,
        string remark)
    {
        VendorName = vendorName;
        NormalizedVendorName = normalizedVendorName;
        PriorityLevel = priorityLevel;
        Remark = remark;
    }

    /// <summary>
    /// 由采购计划子表刷新最近采购汇总。
    /// </summary>
    public void UpdateLatestPurchaseSummary(DateTime? latestPurchaseTime, string latestPurchasePlanName)
    {
        LatestPurchaseTime = latestPurchaseTime;
        LatestPurchasePlanName = latestPurchasePlanName;
    }

    public CrmTransferRecord ChangeOwner(Guid? toOwnerUserId, Guid? operatorUserId, string? remark)
    {
        var record = CrmTransferRecord.CreateOwnerChange(
            CrmTransferEntityType.Vendor,
            Id,
            OwnerUserId,
            toOwnerUserId,
            operatorUserId,
            remark?.Trim() ?? string.Empty);

        OwnerUserId = toOwnerUserId;
        return record;
    }

    public void UpdateFollowSummary(DateTime followAt, string followResult, DateTime? nextFollowAt)
    {
        LastFollowAt = followAt;
        LastFollowResult = followResult;
        NextFollowAt = nextFollowAt;
    }
}


