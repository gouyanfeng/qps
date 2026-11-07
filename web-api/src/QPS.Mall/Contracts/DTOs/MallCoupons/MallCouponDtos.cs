namespace QPS.Mall.Contracts.DTOs.MallCoupons;

public class MallCouponDto
{
    public Guid Id { get; set; }
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
    public string CouponType { get; set; } = string.Empty;
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

/// <summary>确认页按结算商品查询券的请求。</summary>
public class MallCheckoutCouponQueryRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "请选择要结算的商品")]
    [System.ComponentModel.DataAnnotations.MinLength(1, ErrorMessage = "请选择要结算的商品")]
    public List<MallCheckoutCouponItem> Items { get; set; } = new();
}

public class MallCheckoutCouponItem
{
    public Guid SkuId { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "商品数量必须大于 0")]
    public int Quantity { get; set; }
}

/// <summary>确认页可用券：可用时携带本次实际抵扣金额，不可用时携带原因。</summary>
public class MallCheckoutCouponDto
{
    public Guid Id { get; set; }

    public Guid CouponId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CouponType { get; set; } = string.Empty;

    public decimal ThresholdAmount { get; set; }

    public decimal FaceValue { get; set; }

    public decimal? MaxDiscount { get; set; }

    public string ScopeType { get; set; } = "all";

    public DateTime ExpireAt { get; set; }

    public bool IsAvailable { get; set; }

    public string? UnavailableReason { get; set; }

    /// <summary>本次订单可抵扣金额（不可用时为 0）。</summary>
    public decimal DiscountAmount { get; set; }
}