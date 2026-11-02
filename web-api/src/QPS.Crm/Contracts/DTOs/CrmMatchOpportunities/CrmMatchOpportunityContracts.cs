namespace QPS.Crm.Contracts.DTOs.CrmMatchOpportunities;

public class CrmMatchSupplyCandidateDto
{
    public Guid HerbBaseSupplyId { get; set; }
    public Guid HerbBaseId { get; set; }
    public Guid? HerbBaseSubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string HerbBaseName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? AvailableQuantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public decimal? ExpectedPrice { get; set; }
    public string PriceUnit { get; set; } = string.Empty;
    public string SupplyCycle { get; set; } = string.Empty;
    public DateTime? ValidUntil { get; set; }
}
