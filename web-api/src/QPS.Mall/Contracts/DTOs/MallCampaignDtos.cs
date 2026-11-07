namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 活动状态（服务端按服务器本地时间计算）。
/// </summary>
public static class MallCampaignStatus
{
    public const string Upcoming = "Upcoming";
    public const string Ongoing = "Ongoing";
    public const string Ended = "Ended";

    public static string Resolve(DateTime startAt, DateTime endAt, DateTime now)
    {
        if (now < startAt)
        {
            return Upcoming;
        }

        return now < endAt ? Ongoing : Ended;
    }
}

/// <summary>
/// 管理端活动专题列表项。
/// </summary>
public class MallCampaignListItemDto
{
    public Guid Id { get; set; }

    public int Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CoverImageUrl { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    /// <summary>Upcoming / Ongoing / Ended。</summary>
    public string Status { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public int ProductCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 管理端活动专题详情，含已选商品。
/// </summary>
public class MallCampaignDto
{
    public Guid Id { get; set; }

    public int Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CoverImageUrl { get; set; } = string.Empty;

    public string ContentHtml { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<MallCampaignProductItemDto> Items { get; set; } = new();

    /// <summary>领券型活动绑定的券（商品推荐型为空）。</summary>
    public List<MallCampaignCouponDto> Coupons { get; set; } = new();
}

/// <summary>
/// 活动专题绑定的券（管理端与移动端共用展示字段）。
/// </summary>
public class MallCampaignCouponDto
{
    public Guid Id { get; set; }

    public Guid CouponId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

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

    public bool IsActive { get; set; }

    /// <summary>当前是否还能领取（券启用、有库存、在券自身有效期内）。</summary>
    public bool IsClaimable { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>
/// 管理端选择券时的候选项。
/// </summary>
public class MallCampaignCouponOptionDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string CouponType { get; set; } = string.Empty;

    public decimal ThresholdAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal? MaxDiscount { get; set; }

    public int TotalCount { get; set; }

    public int RemainCount { get; set; }

    public int PerLimit { get; set; }

    public string ScopeType { get; set; } = "all";

    public bool IsActive { get; set; }
}

/// <summary>
/// 管理端活动专题选品行。
/// </summary>
public class MallCampaignProductItemDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string MainImageUrl { get; set; } = string.Empty;

    /// <summary>商品最低售价（按启用规格取最小值，仅用于后台展示）。</summary>
    public decimal? MinPrice { get; set; }

    public bool ProductIsActive { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

/// <summary>
/// 管理端新增活动专题请求。
/// </summary>
public class MallCampaignCreateRequest
{
    /// <summary>活动类型，本期支持 0（商品推荐型）/ 2（领券型）。</summary>
    public int Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CoverImageUrl { get; set; } = string.Empty;

    public string ContentHtml { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public List<MallCampaignProductRequest> Items { get; set; } = new();

    /// <summary>领券型活动选择的券 Id 列表（商品推荐型忽略）。</summary>
    public List<Guid> CouponIds { get; set; } = new();
}

/// <summary>
/// 管理端编辑活动专题请求。
/// </summary>
public class MallCampaignUpdateRequest
{
    /// <summary>活动类型，本期支持 0（商品推荐）/ 2（领券）。</summary>
    public int Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CoverImageUrl { get; set; } = string.Empty;

    public string ContentHtml { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int SortOrder { get; set; }

    public List<MallCampaignProductRequest> Items { get; set; } = new();

    /// <summary>领券型活动选择的券 Id 列表（商品推荐型忽略）。</summary>
    public List<Guid> CouponIds { get; set; } = new();
}

/// <summary>
/// 活动专题选品请求行。
/// </summary>
public class MallCampaignProductRequest
{
    public Guid ProductId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

/// <summary>
/// 活动专题上下架请求。
/// </summary>
public class MallCampaignStatusRequest
{
    public bool IsActive { get; set; }
}

/// <summary>
/// 移动端活动专题列表项（仅进行中活动）。
/// </summary>
public class MallCampaignPublicListItemDto
{
    public Guid Id { get; set; }

    public int Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CoverImageUrl { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

/// <summary>
/// 移动端活动专题详情。
/// </summary>
public class MallCampaignPublicDetailDto
{
    public Guid Id { get; set; }

    public int Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CoverImageUrl { get; set; } = string.Empty;

    public string ContentHtml { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string Status { get; set; } = string.Empty;

    /// <summary>领券型活动绑定的券，商品推荐型为空数组。</summary>
    public List<MallCampaignCouponDto> Coupons { get; set; } = new();
}

/// <summary>
/// 移动端活动专题商品行：复用商品列表 DTO，附带分组信息。
/// </summary>
public class MallCampaignProductDto
{
    public Guid CampaignProductId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <summary>商品数据与 GET /api/mall/products 完全一致（含促销富化结果）。</summary>
    public MallProductDto Product { get; set; } = new();
}
