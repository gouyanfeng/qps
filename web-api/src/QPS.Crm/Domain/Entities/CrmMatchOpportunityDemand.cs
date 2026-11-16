using QPS.Shared.Common;

namespace QPS.Crm.Domain.Entities;

/// <summary>
/// 撮合机会关联的药厂采购需求。一个机会可关联多个药厂需求；
/// Role 标识该药厂是发起方还是候选方。
/// </summary>
public class CrmMatchOpportunityDemand : BaseEntity
{
    public const string Initiator = "initiator";
    public const string Candidate = "candidate";
    public const string PendingEvaluation = "pending";
    public const string Kept = "kept";
    public const string Selected = "selected";
    public const string Eliminated = "eliminated";

    public Guid OpportunityId { get; private set; }
    public Guid DemandItemId { get; private set; }
    public Guid VendorId { get; private set; }
    public string VendorName { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal? Quantity { get; private set; }
    public string QuantityUnit { get; private set; } = string.Empty;
    public string Specification { get; private set; } = string.Empty;
    public string QualityRequirement { get; private set; } = string.Empty;
    public decimal? TargetPrice { get; private set; }
    public string PriceUnit { get; private set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; private set; }
    public string DeliveryAddress { get; private set; } = string.Empty;
    public string Role { get; private set; } = Candidate;
    public string Status { get; private set; } = PendingEvaluation;
    public string EliminationReason { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    private CrmMatchOpportunityDemand()
    {
    }

    public static CrmMatchOpportunityDemand Create(
        Guid opportunityId,
        Guid demandItemId,
        Guid vendorId,
        string vendorName,
        string productName,
        decimal? quantity,
        string quantityUnit,
        string specification,
        string qualityRequirement,
        decimal? targetPrice,
        string priceUnit,
        DateTime? expectedDeliveryAt,
        string deliveryAddress,
        string role,
        int sortOrder)
    {
        if (opportunityId == Guid.Empty) throw new InvalidOperationException("Opportunity is required");
        if (demandItemId == Guid.Empty) throw new InvalidOperationException("Demand item is required");
        if (vendorId == Guid.Empty) throw new InvalidOperationException("Vendor is required");

        return new CrmMatchOpportunityDemand
        {
            OpportunityId = opportunityId,
            DemandItemId = demandItemId,
            VendorId = vendorId,
            VendorName = Trim(vendorName),
            ProductName = Trim(productName),
            Quantity = quantity,
            QuantityUnit = Trim(quantityUnit),
            Specification = Trim(specification),
            QualityRequirement = Trim(qualityRequirement),
            TargetPrice = targetPrice,
            PriceUnit = Trim(priceUnit),
            ExpectedDeliveryAt = expectedDeliveryAt,
            DeliveryAddress = Trim(deliveryAddress),
            Role = role == Initiator ? Initiator : Candidate,
            Status = PendingEvaluation,
            SortOrder = sortOrder
        };
    }

    public void Update(decimal? quantity, string specification, string qualityRequirement, decimal? targetPrice, DateTime? expectedDeliveryAt)
    {
        if (quantity.HasValue && quantity.Value < 0) throw new InvalidOperationException("Quantity cannot be negative");
        if (targetPrice.HasValue && targetPrice.Value < 0) throw new InvalidOperationException("Target price cannot be negative");
        Quantity = quantity;
        Specification = Trim(specification);
        QualityRequirement = Trim(qualityRequirement);
        TargetPrice = targetPrice;
        ExpectedDeliveryAt = expectedDeliveryAt;
    }

    public void Keep()
    {
        Status = Kept;
        EliminationReason = string.Empty;
    }

    public void Select()
    {
        Status = Selected;
        EliminationReason = string.Empty;
    }

    public void Eliminate(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Elimination reason is required");
        Status = Eliminated;
        EliminationReason = reason.Trim();
    }

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;
}
