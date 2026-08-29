namespace QPS.Application.Contracts.Crm;

public class CrmTransferOwnerChangeRequest
{
    public List<Guid> EntityIds { get; set; } = new();

    public Guid? ToOwnerUserId { get; set; }

    public string? Remark { get; set; }
}
