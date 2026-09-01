namespace QPS.Application.Contracts.Crm;

public class CrmPurchaseDemandSaveRequest
{
    public Guid VendorId { get; set; }
    public string DemandName { get; set; } = string.Empty;
    public DateTime DemandAt { get; set; } = DateTime.Now;
    public Guid? ContactId { get; set; }
    public DateTime? ExpectedDeliveryAt { get; set; }
    public string ReceivingAddress { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public List<CrmPurchaseDemandItemRequest> Items { get; set; } = [];
}
public class CrmPurchaseDemandItemRequest
{
    public Guid? Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public decimal? TargetPrice { get; set; }
    public string PriceUnit { get; set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; set; }
    public string Remark { get; set; } = string.Empty;
}
public class CrmPurchaseDemandStatusRequest { public string Status { get; set; } = string.Empty; public string ClosedReason { get; set; } = string.Empty; }
