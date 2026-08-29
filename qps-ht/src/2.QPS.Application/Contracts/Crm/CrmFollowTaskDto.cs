namespace QPS.Application.Contracts.Crm;

public class CrmFollowTaskDto
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DateTime? LastFollowAt { get; set; }
    public string LastFollowResult { get; set; } = string.Empty;
    public DateTime? NextFollowAt { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class CrmFollowTaskOverviewDto
{
    public int OverdueCount { get; set; }
    public int TodayCount { get; set; }
    public int NoPlanCount { get; set; }
    public int CompletedLast7DaysCount { get; set; }
}

public class CrmFollowTaskResponse
{
    public CrmFollowTaskOverviewDto Overview { get; set; } = new();
    public List<CrmFollowTaskDto> List { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}
