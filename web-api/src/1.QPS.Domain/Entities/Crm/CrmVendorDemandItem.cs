using QPS.Domain.Common;

namespace QPS.Domain.Entities.Crm;

public class CrmVendorDemandItem : BaseEntity
{
    public Guid VendorDemandId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal? Quantity { get; private set; }
    public string QuantityUnit { get; private set; } = string.Empty;
    public string Specification { get; private set; } = string.Empty;
    public string QualityRequirement { get; private set; } = string.Empty;
    public decimal? TargetPrice { get; private set; }
    public string PriceUnit { get; private set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; private set; }
    public string Remark { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public virtual CrmVendorDemand? VendorDemand { get; private set; }
    private CrmVendorDemandItem() { }
    public CrmVendorDemandItem(string productName, decimal? quantity, string quantityUnit, string specification, string qualityRequirement, decimal? targetPrice, string priceUnit, DateTime? expectedDeliveryAt, string remark, int sortOrder)
    { ProductName = productName; Quantity = quantity; QuantityUnit = quantityUnit; Specification = specification; QualityRequirement = qualityRequirement; TargetPrice = targetPrice; PriceUnit = priceUnit; ExpectedDeliveryAt = expectedDeliveryAt; Remark = remark; SortOrder = sortOrder; }
    public void Update(string productName, decimal? quantity, string quantityUnit, string specification, string qualityRequirement, decimal? targetPrice, string priceUnit, DateTime? expectedDeliveryAt, string remark, int sortOrder)
    { ProductName = productName; Quantity = quantity; QuantityUnit = quantityUnit; Specification = specification; QualityRequirement = qualityRequirement; TargetPrice = targetPrice; PriceUnit = priceUnit; ExpectedDeliveryAt = expectedDeliveryAt; Remark = remark; SortOrder = sortOrder; }
    public bool IsValidForActivation() => !string.IsNullOrWhiteSpace(ProductName) && Quantity > 0 && !string.IsNullOrWhiteSpace(QuantityUnit);
}
