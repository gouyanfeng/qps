using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 订单明细，下单时保存商品快照。
/// </summary>
public class MallOrderItem : BaseEntity
{
    /// <summary>
    /// 所属订单。
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// 商品规格。
    /// </summary>
    public Guid SkuId { get; private set; }

    /// <summary>
    /// 商品。
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// 商品名称快照。
    /// </summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>
    /// SKU 编码快照。
    /// </summary>
    public string SkuCode { get; private set; } = string.Empty;

    /// <summary>
    /// 规格名称快照。
    /// </summary>
    public string SpecName { get; private set; } = string.Empty;

    /// <summary>
    /// 下单单价。
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// 数量。
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// 单件重量（克）快照。
    /// </summary>
    public int WeightGram { get; private set; }

    /// <summary>
    /// 明细金额 = 单价 × 数量。
    /// </summary>
    public decimal ItemAmount { get; private set; }

    /// <summary>
    /// 是否赠品。
    /// </summary>
    public bool IsGift { get; private set; }

    /// <summary>
    /// 命中的促销规则 ID。
    /// </summary>
    public Guid? PromotionId { get; private set; }

    /// <summary>
    /// 促销规则名称快照。
    /// </summary>
    public string PromotionName { get; private set; } = string.Empty;

    /// <summary>
    /// 原价快照（折扣/秒杀场景记录原价）。
    /// </summary>
    public decimal? OriginalUnitPrice { get; private set; }

    private MallOrderItem()
    {
    }

    private MallOrderItem(
        Guid orderId,
        Guid skuId,
        Guid productId,
        string productName,
        string skuCode,
        string specName,
        decimal unitPrice,
        int quantity,
        int weightGram,
        bool isGift = false,
        Guid? promotionId = null,
        string promotionName = "",
        decimal? originalUnitPrice = null)
    {
        OrderId = orderId;
        SkuId = skuId;
        ProductId = productId;
        ProductName = productName;
        SkuCode = skuCode;
        SpecName = specName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        WeightGram = weightGram;
        IsGift = isGift;
        PromotionId = promotionId;
        PromotionName = promotionName;
        OriginalUnitPrice = originalUnitPrice;
        ItemAmount = unitPrice * quantity;
    }

    public static MallOrderItem Create(
        Guid orderId,
        Guid skuId,
        Guid productId,
        string productName,
        string skuCode,
        string specName,
        decimal unitPrice,
        int quantity,
        int weightGram,
        bool isGift = false,
        Guid? promotionId = null,
        string promotionName = "",
        decimal? originalUnitPrice = null)
    {
        return new MallOrderItem(
            orderId,
            skuId,
            productId,
            productName,
            skuCode,
            specName,
            unitPrice,
            quantity,
            weightGram,
            isGift,
            promotionId,
            promotionName,
            originalUnitPrice);
    }
}