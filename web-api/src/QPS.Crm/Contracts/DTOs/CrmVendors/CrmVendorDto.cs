using QPS.Crm.Contracts.DTOs.CrmContacts;
using QPS.Crm.Contracts.DTOs.CrmHerbBaseSubjects;
using QPS.Crm.Contracts.DTOs.CrmTransfers;

namespace QPS.Crm.Contracts.DTOs.CrmVendors;

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

    /// <summary>该厂商作为采购方的历史成交记录（撮合成功关闭且中选）。</summary>
    public List<CrmSubjectDealDto> Deals { get; set; } = new();
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
    public CrmVendorDemandMatchProgressDto MatchProgress { get; set; } = new();
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
    public Guid? MatchOpportunityId { get; set; }
    public string MatchOpportunityStatus { get; set; } = string.Empty;
    public int MatchCandidateCount { get; set; }
    public string MatchDisplayStatus { get; set; } = "待撮合";
    /// <summary>已中选数量：撮合成功关闭机会中选中的实际数量（中选供应可供量或中选需求明细数量）。</summary>
    public decimal MatchedQuantity { get; set; }
    /// <summary>剩余可继续撮合数量，需求全量完成时不再允许发起撮合。</summary>
    public decimal RemainingQuantity { get; set; }
    /// <summary>占用状态：占用 / 空闲。进入任一未关闭撮合机会名单即占用。</summary>
    public string OccupancyStatus { get; set; } = "空闲";
    public bool IsOccupied { get; set; }
    /// <summary>撮合状态：成交 / 撮合中 / 空闲。发起或候选均算撮合中，成交关闭后为成交。</summary>
    public string MatchStatus { get; set; } = "空闲";
    /// <summary>占用该明细的未关闭撮合机会，供候选列表区分是否为自己。</summary>
    public List<Guid> OccupiedOpportunityIds { get; set; } = new();
}

public class CrmVendorDemandMatchProgressDto
{
    public int TotalItemCount { get; set; }
    public int StartedItemCount { get; set; }
    public int PendingItemCount { get; set; }
    public int InProgressItemCount { get; set; }
    public int PausedItemCount { get; set; }
    public int ClosedItemCount { get; set; }
    public int CompletedItemCount { get; set; }
}

/// <summary>采购明细列表行：一条采购明细及其所属单据信息，供采购列表按品类一行展示。</summary>
public class CrmVendorDemandItemRowDto
{
    public CrmVendorDemandBriefDto Demand { get; set; } = new();
    public CrmVendorDemandItemDto Item { get; set; } = new();
}

/// <summary>采购明细行所属单据的展示字段。</summary>
public class CrmVendorDemandBriefDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string DemandNo { get; set; } = string.Empty;
    public string DemandName { get; set; } = string.Empty;
    public DateTime DemandAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; set; }
    public string ReceivingAddress { get; set; } = string.Empty;
    public CrmVendorDemandMatchProgressDto MatchProgress { get; set; } = new();
}


