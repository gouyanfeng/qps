namespace QPS.Application.Contracts.Crm;

public class CrmHerbBaseSupplyDto
{
    public Guid Id { get; set; }
    public Guid HerbBaseId { get; set; }
    public Guid? HerbBaseSubjectId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal? AvailableQuantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public string HarvestSeason { get; set; } = string.Empty;
    public decimal? ExpectedPrice { get; set; }
    public string PriceUnit { get; set; } = string.Empty;
    public string SupplyCycle { get; set; } = string.Empty;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
}

public class CrmHerbBaseSupplySaveRequest
{
    public string ProductName { get; set; } = string.Empty;
    public decimal? AvailableQuantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public string HarvestSeason { get; set; } = string.Empty;
    public decimal? ExpectedPrice { get; set; }
    public string PriceUnit { get; set; } = string.Empty;
    public string SupplyCycle { get; set; } = string.Empty;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string Remark { get; set; } = string.Empty;
}

public class CrmHerbBaseSupplyStatusRequest { public string Status { get; set; } = string.Empty; }
