using QPS.Domain.Common;

namespace QPS.Domain.Entities.Crm;

public class CrmMatchCandidate : BaseEntity
{
    public const string PendingEvaluation = "待评估";
    public const string Kept = "保留";
    public const string Eliminated = "淘汰";

    public Guid MatchOpportunityId { get; private set; }
    public Guid HerbBaseSupplyId { get; private set; }
    public Guid HerbBaseId { get; private set; }
    public Guid? HerbBaseSubjectId { get; private set; }
    public string SubjectName { get; private set; } = string.Empty;
    public string HerbBaseName { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal? AvailableQuantity { get; private set; }
    public string QuantityUnit { get; private set; } = string.Empty;
    public string Specification { get; private set; } = string.Empty;
    public string QualityRequirement { get; private set; } = string.Empty;
    public decimal? ExpectedPrice { get; private set; }
    public string PriceUnit { get; private set; } = string.Empty;
    public string SupplyCycle { get; private set; } = string.Empty;
    public DateTime? ValidUntil { get; private set; }
    public string Status { get; private set; } = PendingEvaluation;
    public string EliminationReason { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    private CrmMatchCandidate()
    {
    }

    public static CrmMatchCandidate Create(
        Guid matchOpportunityId,
        Guid herbBaseSupplyId,
        Guid herbBaseId,
        Guid? herbBaseSubjectId,
        string subjectName,
        string herbBaseName,
        string productName,
        decimal? availableQuantity,
        string quantityUnit,
        string specification,
        string qualityRequirement,
        decimal? expectedPrice,
        string priceUnit,
        string supplyCycle,
        DateTime? validUntil,
        int sortOrder)
    {
        return new CrmMatchCandidate
        {
            MatchOpportunityId = matchOpportunityId,
            HerbBaseSupplyId = herbBaseSupplyId,
            HerbBaseId = herbBaseId,
            HerbBaseSubjectId = herbBaseSubjectId,
            SubjectName = Trim(subjectName),
            HerbBaseName = Trim(herbBaseName),
            ProductName = Trim(productName),
            AvailableQuantity = availableQuantity,
            QuantityUnit = Trim(quantityUnit),
            Specification = Trim(specification),
            QualityRequirement = Trim(qualityRequirement),
            ExpectedPrice = expectedPrice,
            PriceUnit = Trim(priceUnit),
            SupplyCycle = Trim(supplyCycle),
            ValidUntil = validUntil,
            Status = PendingEvaluation,
            SortOrder = sortOrder
        };
    }

    public void Keep()
    {
        Status = Kept;
        EliminationReason = string.Empty;
    }

    public void Eliminate(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("淘汰撮合候选必须填写淘汰原因");
        }

        Status = Eliminated;
        EliminationReason = reason.Trim();
    }

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;
}
