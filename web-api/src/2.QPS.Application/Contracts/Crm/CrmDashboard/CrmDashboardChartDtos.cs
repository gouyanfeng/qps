namespace QPS.Application.Contracts.Crm.CrmDashboard;

public class CrmDashboardChartItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class CrmDashboardTrendItemDto
{
    public DateTime Date { get; set; }
    public int FollowCount { get; set; }
    public int EffectiveFollowCount { get; set; }
}

public class CrmDashboardNewBaseTrendItemDto
{
    public DateTime Date { get; set; }
    public int NewBaseCount { get; set; }
}

public class CrmDashboardNewPurchaseDemandTrendItemDto
{
    public DateTime Date { get; set; }
    public int NewPurchaseDemandCount { get; set; }
}
