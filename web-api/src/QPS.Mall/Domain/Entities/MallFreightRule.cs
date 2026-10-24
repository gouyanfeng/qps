using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 门店运费规则：按重量计费，可配置满额包邮。
/// </summary>
public class MallFreightRule : BaseEntity
{
    /// <summary>
    /// 所属门店，每个门店一条规则。
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// 首重重量（克）。
    /// </summary>
    public int FirstWeightGram { get; private set; }

    /// <summary>
    /// 首重价格。
    /// </summary>
    public decimal FirstWeightFee { get; private set; }

    /// <summary>
    /// 续重步长（克）。
    /// </summary>
    public int AdditionalWeightGram { get; private set; }

    /// <summary>
    /// 每个续重步长的价格。
    /// </summary>
    public decimal AdditionalWeightFee { get; private set; }

    /// <summary>
    /// 满额包邮门槛，为空表示不包邮。
    /// </summary>
    public decimal? FreeShippingAmount { get; private set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsActive { get; private set; }

    private MallFreightRule()
    {
    }

    private MallFreightRule(
        Guid storeId,
        int firstWeightGram,
        decimal firstWeightFee,
        int additionalWeightGram,
        decimal additionalWeightFee,
        decimal? freeShippingAmount,
        bool isActive)
    {
        StoreId = storeId;
        FirstWeightGram = firstWeightGram;
        FirstWeightFee = firstWeightFee;
        AdditionalWeightGram = additionalWeightGram;
        AdditionalWeightFee = additionalWeightFee;
        FreeShippingAmount = freeShippingAmount;
        IsActive = isActive;
    }

    public static MallFreightRule Create(
        Guid storeId,
        int firstWeightGram,
        decimal firstWeightFee,
        int additionalWeightGram,
        decimal additionalWeightFee,
        decimal? freeShippingAmount,
        bool isActive)
    {
        return new MallFreightRule(
            storeId,
            firstWeightGram,
            firstWeightFee,
            additionalWeightGram,
            additionalWeightFee,
            freeShippingAmount,
            isActive);
    }

    public void Update(
        int firstWeightGram,
        decimal firstWeightFee,
        int additionalWeightGram,
        decimal additionalWeightFee,
        decimal? freeShippingAmount,
        bool isActive)
    {
        FirstWeightGram = firstWeightGram;
        FirstWeightFee = firstWeightFee;
        AdditionalWeightGram = additionalWeightGram;
        AdditionalWeightFee = additionalWeightFee;
        FreeShippingAmount = freeShippingAmount;
        IsActive = isActive;
    }
}
