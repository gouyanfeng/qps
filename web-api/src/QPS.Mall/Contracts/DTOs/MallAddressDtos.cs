using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Contracts.DTOs;

public class MallAddressDto
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string ProvinceName { get; set; } = string.Empty;

    public string CityName { get; set; } = string.Empty;

    public string DistrictName { get; set; } = string.Empty;

    public string DetailAddress { get; set; } = string.Empty;

    public string FullAddress { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class MallAddressCreateRequest
{
    public Guid? UserId { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string ProvinceName { get; set; } = string.Empty;

    public string CityName { get; set; } = string.Empty;

    public string DistrictName { get; set; } = string.Empty;

    public string DetailAddress { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

public class MallAddressUpdateRequest
{
    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string ProvinceName { get; set; } = string.Empty;

    public string CityName { get; set; } = string.Empty;

    public string DistrictName { get; set; } = string.Empty;

    public string DetailAddress { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

public class MallFreightPreviewRequest
{
    public List<MallFreightPreviewItemRequest> Items { get; set; } = new();

    public string ProvinceName { get; set; } = string.Empty;

    public string CityName { get; set; } = string.Empty;
}

public class MallFreightPreviewItemRequest
{
    public Guid SkuId { get; set; }

    public int Quantity { get; set; }
}

public class MallFreightPreviewDto
{
    public decimal TotalFreight { get; set; }

    public List<MallFreightPreviewGroupDto> Groups { get; set; } = new();
}

public class MallFreightPreviewGroupDto
{
    public string TemplateName { get; set; } = string.Empty;

    public FreightBillingType BillingType { get; set; }

    public string RegionName { get; set; } = string.Empty;

    public decimal GoodsAmount { get; set; }

    public decimal Freight { get; set; }

    public bool IsFreeShipping { get; set; }
}

public class MallPromotionDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string TriggerType { get; set; } = string.Empty;

    public string TriggerConfigJson { get; set; } = string.Empty;

    public string ActionType { get; set; } = string.Empty;

    public string ActionConfigJson { get; set; } = string.Empty;

    public string ScopeType { get; set; } = string.Empty;

    public int MaxUsagePerUser { get; set; }

    public int MaxUsagePerOrder { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<MallPromotionScopeDto> Scopes { get; set; } = new();
}

public class MallPromotionScopeDto
{
    public Guid Id { get; set; }

    public string ScopeTargetType { get; set; } = string.Empty;

    public Guid ScopeTargetId { get; set; }

    public string ScopeTargetName { get; set; } = string.Empty;
}

public class MallPromotionCreateRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string TriggerType { get; set; } = string.Empty;

    public string TriggerConfigJson { get; set; } = "{}";

    public string ActionType { get; set; } = string.Empty;

    public string ActionConfigJson { get; set; } = "{}";

    public string ScopeType { get; set; } = "Global";

    public int MaxUsagePerUser { get; set; }

    public int MaxUsagePerOrder { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public List<MallPromotionScopeRequest> Scopes { get; set; } = new();
}

public class MallPromotionUpdateRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string TriggerType { get; set; } = string.Empty;

    public string TriggerConfigJson { get; set; } = "{}";

    public string ActionType { get; set; } = string.Empty;

    public string ActionConfigJson { get; set; } = "{}";

    public string ScopeType { get; set; } = "Global";

    public int MaxUsagePerUser { get; set; }

    public int MaxUsagePerOrder { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public List<MallPromotionScopeRequest> Scopes { get; set; } = new();
}

public class MallPromotionScopeRequest
{
    public string ScopeTargetType { get; set; } = string.Empty;

    public Guid ScopeTargetId { get; set; }
}