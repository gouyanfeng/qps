namespace QPS.Crm.Contracts.DTOs.CrmTransfers;

public class CrmTransferOwnerChangeRequest
{
    public List<Guid> EntityIds { get; set; } = new();

    public Guid? ToOwnerUserId { get; set; }

    public string? Remark { get; set; }
}
