namespace QPS.Application.Contracts.Crm.CrmHerbBaseSupplies;

public class CrmHerbBaseSupplyListDto : CrmHerbBaseSupplyDto
{
    public string SubjectName { get; set; } = string.Empty;
    public string HerbBaseName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
    public int SelectedCount { get; set; }
    public string OccupiedDemandSummary { get; set; } = string.Empty;
}
