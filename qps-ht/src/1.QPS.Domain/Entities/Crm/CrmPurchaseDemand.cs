using QPS.Domain.Common;

namespace QPS.Domain.Entities.Crm;

public class CrmPurchaseDemand : BaseEntity
{
    public const string Pending = "待确认";
    public const string Active = "有效";
    public const string Matching = "匹配中";
    public const string Completed = "已完成";
    public const string Closed = "已关闭";
    public Guid VendorId { get; private set; }
    public Guid? ContactId { get; private set; }
    public string DemandNo { get; private set; } = string.Empty;
    public string DemandName { get; private set; } = string.Empty;
    public DateTime DemandAt { get; private set; }
    public string Status { get; private set; } = Pending;
    public string SourceType { get; private set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; private set; }
    public string ReceivingAddress { get; private set; } = string.Empty;
    public string SourceUrl { get; private set; } = string.Empty;
    public string Remark { get; private set; } = string.Empty;
    public string ClosedReason { get; private set; } = string.Empty;
    public virtual CrmVendor? Vendor { get; private set; }
    public virtual CrmContact? Contact { get; private set; }
    public virtual ICollection<CrmPurchaseDemandItem> Items { get; private set; } = new List<CrmPurchaseDemandItem>();
    private CrmPurchaseDemand() { }
    public static CrmPurchaseDemand Create(Guid vendorId, string demandNo, string demandName, DateTime demandAt, string sourceType, Guid? contactId, DateTime? expectedDeliveryAt, string receivingAddress, string sourceUrl, string remark, IReadOnlyCollection<CrmPurchaseDemandItem> items)
    {
        var demand = new CrmPurchaseDemand { DemandNo = demandNo, SourceType = sourceType };
        demand.UpdateCore(vendorId, demandName, demandAt, contactId, expectedDeliveryAt, receivingAddress, sourceUrl, remark, items);
        return demand;
    }
    public void Update(Guid vendorId, string demandName, DateTime demandAt, Guid? contactId, DateTime? expectedDeliveryAt, string receivingAddress, string sourceUrl, string remark, IReadOnlyCollection<CrmPurchaseDemandItem> items)
    {
        if (Status is Completed or Closed) throw new InvalidOperationException("终态采购需求不可编辑");
        UpdateCore(vendorId, demandName, demandAt, contactId, expectedDeliveryAt, receivingAddress, sourceUrl, remark, items);
    }
    public void ChangeStatus(string targetStatus, string? closedReason)
    {
        var permitted = (Status, targetStatus) is (Pending, Active) or (Pending, Closed) or (Active, Matching) or (Active, Closed) or (Matching, Active) or (Matching, Completed) or (Matching, Closed);
        if (!permitted) throw new InvalidOperationException("不允许的采购需求状态流转");
        if (targetStatus == Active && (Items.Count == 0 || Items.Any(item => !item.IsValidForActivation()))) throw new InvalidOperationException("转为有效前请补齐每条明细的品类、数量和单位");
        if (targetStatus == Closed && string.IsNullOrWhiteSpace(closedReason)) throw new InvalidOperationException("关闭采购需求必须填写关闭原因");
        Status = targetStatus;
        ClosedReason = targetStatus == Closed ? closedReason!.Trim() : string.Empty;
    }
    private void UpdateCore(Guid vendorId, string demandName, DateTime demandAt, Guid? contactId, DateTime? expectedDeliveryAt, string receivingAddress, string sourceUrl, string remark, IReadOnlyCollection<CrmPurchaseDemandItem> items)
    {
        VendorId = vendorId; DemandName = demandName; DemandAt = demandAt; ContactId = contactId; ExpectedDeliveryAt = expectedDeliveryAt;
        ReceivingAddress = receivingAddress ?? string.Empty;
        SourceUrl = sourceUrl ?? string.Empty;
        Remark = remark ?? string.Empty;

        // 保持集合引用不变，使 EF Core 只跟踪一次被移除的旧明细。
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }
}

