namespace QPS.Application.Contracts.Crm;

public class CrmVendorUpdateRequest
{
    public string VendorName { get; set; } = string.Empty;

    public string PriorityLevel { get; set; } = "Medium";

    public string Remark { get; set; } = string.Empty;
}
