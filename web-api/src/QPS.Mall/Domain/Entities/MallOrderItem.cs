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
    /// 商品主图快照。
    /// </summary>
    public string ProductImage { get; private set; } = string.Empty;

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
    /// 原价快照，恒有值：无促销时与 <see cref="UnitPrice"/> 相同。
    /// </summary>
    public decimal OriginalUnitPrice { get; private set; }

    /// <summary>
    /// 本行实际促销优惠金额（各条命中促销分摊到本行的优惠合计），赠品为 0。
    /// </summary>
    public decimal PromotionDiscountAmount { get; private set; }

    /// <summary>
    /// 本行命中的促销快照，JSON 数组：[{id, name, actionType, amount}]。
    /// 仅存放结构化数据，展示文案由前端自行拼装。
    /// </summary>
    public string PromotionSnapshot { get; private set; } = "[]";

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
        decimal originalUnitPrice = 0,
        string productImage = "",
        decimal promotionDiscountAmount = 0,
        string promotionSnapshot = "[]")
    {
        OrderId = orderId;
        SkuId = skuId;
        ProductId = productId;
        ProductName = productName;
        SkuCode = skuCode;
        SpecName = specName;
        ProductImage = productImage;
        UnitPrice = unitPrice;
        Quantity = quantity;
        WeightGram = weightGram;
        IsGift = isGift;
        PromotionId = promotionId;
        PromotionName = promotionName;
        // 原价恒写：未提供原价（无促销）时与成交单价一致
        OriginalUnitPrice = originalUnitPrice > 0 ? originalUnitPrice : unitPrice;
        PromotionDiscountAmount = promotionDiscountAmount;
        PromotionSnapshot = string.IsNullOrWhiteSpace(promotionSnapshot) ? "[]" : promotionSnapshot;
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
        decimal originalUnitPrice = 0,
        string productImage = "",
        decimal promotionDiscountAmount = 0,
        string promotionSnapshot = "[]")
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
            originalUnitPrice,
            productImage,
            promotionDiscountAmount,
            promotionSnapshot);
    }
}