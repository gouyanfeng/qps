namespace QPS.Application.Contracts.Crm;

public class CrmVendorCreateRequest
{
    public string VendorName { get; set; } = string.Empty;

    public string PriorityLevel { get; set; } = "Medium";

    public Guid? OwnerUserId { get; set; }

    public string Remark { get; set; } = string.Empty;
}
