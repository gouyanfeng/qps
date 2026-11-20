namespace QPS.Crm.Contracts.DTOs.CrmHerbBaseSupplies;

public class CrmHerbBaseSupplyListDto : CrmHerbBaseSupplyDto
{
    public string SubjectName { get; set; } = string.Empty;
    public string HerbBaseName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
    public int SelectedCount { get; set; }
    /// <summary>撮合状态：成交 / 撮合中 / 空闲。出现在任一未关闭撮合机会（发起或候选）即撮合中，成交关闭后为成交。</summary>
    public string MatchStatus { get; set; } = "空闲";
    public string OccupiedDemandSummary { get; set; } = string.Empty;
    /// <summary>撮合中关联的未关闭撮合机会 ID，用于从供应列表直接进入撮合详情。</summary>
    public List<Guid> OccupiedOpportunityIds { get; set; } = [];
    /// <summary>最近一次参与的撮合机会 ID（含已关闭的成交/结束记录），用于从供应列表查看历史撮合详情。</summary>
    public Guid? LatestOpportunityId { get; set; }
}
