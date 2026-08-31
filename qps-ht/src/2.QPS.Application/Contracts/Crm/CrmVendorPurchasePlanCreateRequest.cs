namespace QPS.Application.Contracts.Crm;

public class CrmVendorPurchasePlanCreateRequest
{
    public string PurchasePlanName { get; set; } = string.Empty;

    public DateTime? PurchaseTime { get; set; }

    public List<string>? ProductNames { get; set; }

    public string PageUrl { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;
}
