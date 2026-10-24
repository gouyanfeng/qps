namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 门店运费规则展示数据。
/// </summary>
public class MallFreightRuleDto
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public int FirstWeightGram { get; set; }

    public decimal FirstWeightFee { get; set; }

    public int AdditionalWeightGram { get; set; }

    public decimal AdditionalWeightFee { get; set; }

    public decimal? FreeShippingAmount { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 新增或更新门店运费规则请求。
/// </summary>
public class MallFreightRuleUpsertRequest
{
    public int FirstWeightGram { get; set; }

    public decimal FirstWeightFee { get; set; }

    public int AdditionalWeightGram { get; set; }

    public decimal AdditionalWeightFee { get; set; }

    public decimal? FreeShippingAmount { get; set; }

    public bool IsActive { get; set; } = true;
}
