using QPS.Crm.Contracts.DTOs.CrmHerbBases;
using QPS.Crm.Contracts.DTOs.CrmContacts;
using QPS.Crm.Contracts.DTOs.CrmFollowRecords;
using QPS.Crm.Contracts.DTOs.CrmTransfers;

namespace QPS.Crm.Contracts.DTOs.CrmHerbBaseSubjects;

public class CrmHerbBaseSubjectDto
{
    public Guid Id { get; set; }
    public string? SubjectName { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public Guid? OwnerUserId { get; set; }
    public string? OwnerUserName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public int Score { get; set; }
    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactPhone { get; set; }
    public DateTime? LastFollowAt { get; set; }
    public string? LastFollowResult { get; set; }
    public DateTime? NextFollowAt { get; set; }
    public string? Remark { get; set; }
    public int BaseCount { get; set; }
    public decimal TotalScale { get; set; }
    public List<string> ProductName { get; set; } = new();
    public List<string> Regions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CrmSubjectDealDto
{
    public Guid Id { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BaseName { get; set; } = string.Empty;
    public string CloseResult { get; set; } = string.Empty;
    public string ClosedReason { get; set; } = string.Empty;
    public DateTime ClosedAt { get; set; }
}

public class CrmHerbBaseSubjectDetailDto : CrmHerbBaseSubjectDto
{
    public List<CrmHerbBaseDto> HerbBases { get; set; } = new();
    public List<CrmContactDto> Contacts { get; set; } = new();
    public List<CrmFollowRecordDto> FollowRecords { get; set; } = new();
    public List<CrmTransferRecordDto> TransferRecords { get; set; } = new();
    public List<CrmSubjectDealDto> Deals { get; set; } = new();
}
