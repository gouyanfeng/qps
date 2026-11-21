using QPS.Shared.Common;

namespace QPS.Crm.Domain.Entities;

public class CrmVendorDemand : BaseEntity
{
    public Guid VendorId { get; private set; }
    public Guid? ContactId { get; private set; }
    public string DemandNo { get; private set; } = string.Empty;
    public string DemandName { get; private set; } = string.Empty;
    public DateTime DemandAt { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; private set; }
    public string ReceivingAddress { get; private set; } = string.Empty;
    public string SourceUrl { get; private set; } = string.Empty;
    public string Remark { get; private set; } = string.Empty;
    public virtual CrmVendor? Vendor { get; private set; }
    public virtual CrmContact? Contact { get; private set; }
    public virtual ICollection<CrmVendorDemandItem> Items { get; private set; } = new List<CrmVendorDemandItem>();
    private CrmVendorDemand() { }
    public static CrmVendorDemand Create(Guid vendorId, string demandNo, string demandName, DateTime demandAt, string sourceType, Guid? contactId, DateTime? expectedDeliveryAt, string receivingAddress, string sourceUrl, string remark, IReadOnlyCollection<CrmVendorDemandItem> items)
    {
        var demand = new CrmVendorDemand { DemandNo = demandNo, SourceType = sourceType };
        demand.UpdateCore(vendorId, demandName, demandAt, contactId, expectedDeliveryAt, receivingAddress, sourceUrl, remark, items);
        return demand;
    }
    public void Update(Guid vendorId, string demandName, DateTime demandAt, Guid? contactId, DateTime? expectedDeliveryAt, string receivingAddress, string sourceUrl, string remark, IReadOnlyCollection<CrmVendorDemandItem> items)
    {
        UpdateCore(vendorId, demandName, demandAt, contactId, expectedDeliveryAt, receivingAddress, sourceUrl, remark, items);
    }
    private void UpdateCore(Guid vendorId, string demandName, DateTime demandAt, Guid? contactId, DateTime? expectedDeliveryAt, string receivingAddress, string sourceUrl, string remark, IReadOnlyCollection<CrmVendorDemandItem> items)
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
