namespace QPS.Mall.Contracts.DTOs.MallCoupons;

public class MallCouponDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CouponType { get; set; } = string.Empty;
    public decimal ThresholdAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int TotalCount { get; set; }
    public int RemainCount { get; set; }
    public int PerLimit { get; set; }
    public int ValidDays { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public string ScopeType { get; set; } = "all";
    public string? ScopeValue { get; set; }
    public bool IsActive { get; set; }
}

public class MallCouponCreateRequest
{
    public Guid StoreId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CouponType { get; set; } = string.Empty;
    public decimal ThresholdAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int TotalCount { get; set; }
    public int PerLimit { get; set; }
    public int ValidDays { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public string ScopeType { get; set; } = "all";
    public string? ScopeValue { get; set; }
}

public class MallCouponUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal ThresholdAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int PerLimit { get; set; }
    public int ValidDays { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public string ScopeType { get; set; } = "all";
    public string? ScopeValue { get; set; }
}

public class MallCouponQueryRequest
{
    public Guid? StoreId { get; set; }
    public string? CouponType { get; set; }
    public bool? IsActive { get; set; }
    public string? Keyword { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class MallMemberCouponDto
{
    public Guid Id { get; set; }
    public Guid CouponId { get; set; }
    public Guid MemberId { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public decimal ThresholdAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ExpireAt { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? UsedOrderNo { get; set; }
}

public class MallMemberCouponQueryRequest
{
    public Guid MemberId { get; set; }
    public string? Status { get; set; }
}