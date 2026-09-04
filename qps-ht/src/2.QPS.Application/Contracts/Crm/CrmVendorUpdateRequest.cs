namespace QPS.Application.Contracts.Crm;

public class CrmVendorUpdateRequest
{
    public string VendorName { get; set; } = string.Empty;

    public string PriorityLevel { get; set; } = "中";

    public string Remark { get; set; } = string.Empty;
}
