using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Contracts.DTOs;

public class MallFreightTemplateListDto
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public FreightBillingType BillingType { get; set; }

    public string BillingTypeName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public int RegionCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class MallFreightTemplateDetailDto
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public FreightBillingType BillingType { get; set; }

    public string BillingTypeName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<MallFreightRegionDto> Regions { get; set; } = new();
}

public class MallFreightRegionDto
{
    public Guid Id { get; set; }

    public string RegionName { get; set; } = string.Empty;

    public string ProvinceCodes { get; set; } = string.Empty;

    public string? CityCodes { get; set; }

    public int FirstValue { get; set; }

    public decimal FirstFee { get; set; }

    public int StepValue { get; set; }

    public decimal StepFee { get; set; }

    public decimal? FreeShippingAmount { get; set; }

    public int? FreeShippingQuantity { get; set; }

    public bool IsDefaultRegion { get; set; }
}

public class MallFreightTemplateCreateRequest
{
    public Guid StoreId { get; set; }

    public string Name { get; set; } = string.Empty;

    public FreightBillingType BillingType { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public List<MallFreightRegionRequest> Regions { get; set; } = new();
}

public class MallFreightTemplateUpdateRequest
{
    public string Name { get; set; } = string.Empty;

    public FreightBillingType BillingType { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public List<MallFreightRegionRequest> Regions { get; set; } = new();
}

public class MallFreightRegionRequest
{
    public Guid? Id { get; set; }

    public string RegionName { get; set; } = string.Empty;

    public string ProvinceCodes { get; set; } = string.Empty;

    public string? CityCodes { get; set; }

    public int FirstValue { get; set; }

    public decimal FirstFee { get; set; }

    public int StepValue { get; set; }

    public decimal StepFee { get; set; }

    public decimal? FreeShippingAmount { get; set; }

    public int? FreeShippingQuantity { get; set; }

    public bool IsDefaultRegion { get; set; }
}