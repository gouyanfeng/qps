namespace QPS.Application.Contracts.Crm;

public class CrmVendorDto
{
    public Guid Id { get; set; }

    public string VendorName { get; set; } = string.Empty;

    public string NormalizedVendorName { get; set; } = string.Empty;

    public string PriorityLevel { get; set; } = string.Empty;

    public DateTime? LatestPurchaseTime { get; set; }

    public string LatestPurchaseDemandName { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public Guid? OwnerUserId { get; set; }

    public string? OwnerUserName { get; set; }

    public DateTime? LastFollowAt { get; set; }

    public string LastFollowResult { get; set; } = string.Empty;

    public DateTime? NextFollowAt { get; set; }

    public string PrimaryContactName { get; set; } = string.Empty;

    public string PrimaryContactPhone { get; set; } = string.Empty;

    public int PurchaseDemandCount { get; set; }

    public int ProductCount { get; set; }

    public List<string> ProductName { get; set; } = new();

    public int ContactCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<CrmContactDto> Contacts { get; set; } = new();

    public List<CrmVendorProductDto> Products { get; set; } = new();

    public List<CrmVendorDemandDto> PurchaseDemands { get; set; } = new();

    public List<CrmTransferRecordDto> TransferRecords { get; set; } = new();
}

public class CrmVendorProductDto
{
    public Guid Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string Remark { get; set; } = string.Empty;
}

public class CrmVendorDemandDto
{
    public Guid Id { get; set; }

    public Guid VendorId { get; set; }

    public string DemandNo { get; set; } = string.Empty;
    public string DemandName { get; set; } = string.Empty;
    public DateTime DemandAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public Guid? OwnerUserId { get; set; }
    public string OwnerUserName { get; set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; set; }
    public string ReceivingAddress { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;
    public string ClosedReason { get; set; } = string.Empty;
    public List<CrmVendorDemandItemDto> Items { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
public class CrmVendorDemandItemDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public decimal? TargetPrice { get; set; }
    public string PriceUnit { get; set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; set; }
    public string Remark { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}


