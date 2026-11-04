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

    public int PromotionType { get; set; }

    public decimal ThresholdAmount { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string ActionConfigJson { get; set; } = string.Empty;

    /// <summary>促销标签（关联 MallTags），参与该促销的商品展示时带上。</summary>
    public Guid? PromotionTagId { get; set; }

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

/// <summary>
/// 移动端换购资格（适用源=订单价格的 TradeIn 促销）。
/// </summary>
public class MallExchangeOfferDto
{
    public Guid PromotionId { get; set; }

    public string PromotionName { get; set; } = string.Empty;

    public decimal MinAmount { get; set; }

    public Guid SkuId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

    public string ProductImage { get; set; } = string.Empty;

    public decimal OriginalPrice { get; set; }

    public decimal TradeInPrice { get; set; }

    public int MaxQuantity { get; set; }
}

public class MallPromotionCreateRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>促销类型：0 满减 / 1 折扣 / 2 买赠 / 3 换购。</summary>
    public int PromotionType { get; set; }

    /// <summary>订单门槛金额。</summary>
    public decimal ThresholdAmount { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string ActionConfigJson { get; set; } = "{}";

    /// <summary>促销标签（关联 MallTags，可选）。</summary>
    public Guid? PromotionTagId { get; set; }

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

    public int PromotionType { get; set; }

    public decimal ThresholdAmount { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string ActionConfigJson { get; set; } = "{}";

    /// <summary>促销标签（关联 MallTags，可选）。</summary>
    public Guid? PromotionTagId { get; set; }

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