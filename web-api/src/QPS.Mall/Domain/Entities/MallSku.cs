using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 商品规格（SKU），库存与价格记在此层，全门店共享。
/// </summary>
public class MallSku : BaseEntity
{
    /// <summary>
    /// 所属商品。
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// SKU 编码，唯一。
    /// </summary>
    public string SkuCode { get; private set; } = string.Empty;

    /// <summary>
    /// 规格名称，例如「500g 装」。
    /// </summary>
    public string SpecName { get; private set; } = string.Empty;

    /// <summary>
    /// 单价。
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// 单件重量（克），用于运费计算。
    /// </summary>
    public int WeightGram { get; private set; }

    /// <summary>
    /// 可售库存。
    /// </summary>
    public int Stock { get; private set; }

    /// <summary>
    /// 预警库存，0 表示不预警。
    /// </summary>
    public int WarnStock { get; private set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsActive { get; private set; }

    private MallSku()
    {
    }

    private MallSku(
        Guid productId,
        string skuCode,
        string specName,
        decimal price,
        int weightGram,
        int stock,
        int warnStock,
        bool isActive)
    {
        ProductId = productId;
        SkuCode = skuCode;
        SpecName = specName;
        Price = price;
        WeightGram = weightGram;
        Stock = stock;
        WarnStock = warnStock;
        IsActive = isActive;
    }

    public static MallSku Create(
        Guid productId,
        string skuCode,
        string specName,
        decimal price,
        int weightGram,
        int stock,
        int warnStock,
        bool isActive)
    {
        return new MallSku(productId, skuCode, specName, price, weightGram, stock, warnStock, isActive);
    }

    public void Update(
        string skuCode,
        string specName,
        decimal price,
        int weightGram,
        int stock,
        int warnStock)
    {
        SkuCode = skuCode;
        SpecName = specName;
        Price = price;
        WeightGram = weightGram;
        Stock = stock;
        WarnStock = warnStock;
    }

    public void ChangeStatus(bool isActive)
    {
        IsActive = isActive;
    }

    /// <summary>
    /// 校验可售数量。
    /// </summary>
    public void EnsureStockEnough(int quantity)
    {
        if (Stock < quantity)
        {
            throw new BusinessException(400, $"规格「{SpecName}」库存不足");
        }
    }

    /// <summary>
    /// 扣减库存，返回扣减前库存。
    /// </summary>
    public int DeductStock(int quantity)
    {
        EnsureStockEnough(quantity);

        var beforeStock = Stock;
        Stock -= quantity;

        return beforeStock;
    }

    /// <summary>
    /// 回补库存，返回回补前库存。
    /// </summary>
    public int RestoreStock(int quantity)
    {
        var beforeStock = Stock;
        Stock += quantity;

        return beforeStock;
    }

    /// <summary>
    /// 调整为目标库存，返回调整前库存。
    /// </summary>
    public int AdjustStock(int targetStock)
    {
        var beforeStock = Stock;
        Stock = targetStock;

        return beforeStock;
    }

    /// <summary>
    /// 修改规格价格。
    /// </summary>
    public void ChangePrice(decimal price)
    {
        Price = price;
    }
}
