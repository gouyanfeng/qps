namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 库存管理列表项，按规格维度展示。
/// </summary>
public class MallSkuStockDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string SkuCode { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public int WarnStock { get; set; }

    public bool IsLowStock { get; set; }

    public bool IsActive { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 调整库存请求。
/// </summary>
public class MallSkuAdjustStockRequest
{
    /// <summary>调整后的目标库存。</summary>
    public int TargetStock { get; set; }

    /// <summary>调整备注。</summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 批量调整规格请求，价格与库存至少填写一项。
/// </summary>
public class MallSkuBatchAdjustRequest
{
    public List<Guid> Ids { get; set; } = new();

    /// <summary>统一设置的规格价格，为空表示不修改。</summary>
    public decimal? Price { get; set; }

    /// <summary>统一设置的目标库存，为空表示不修改。</summary>
    public int? TargetStock { get; set; }

    /// <summary>批量调整备注。</summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 库存变更流水展示数据。
/// </summary>
public class MallStockLogDto
{
    public Guid Id { get; set; }

    public Guid SkuId { get; set; }

    public string SkuCode { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

    public string ChangeType { get; set; } = string.Empty;

    public int ChangeQuantity { get; set; }

    public int BeforeStock { get; set; }

    public int AfterStock { get; set; }

    public string? RelatedOrderNo { get; set; }

    public string? Remark { get; set; }

    public string? OperatorName { get; set; }

    public DateTime CreatedAt { get; set; }
}
