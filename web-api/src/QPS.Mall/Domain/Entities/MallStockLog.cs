using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 规格库存变更流水。
/// </summary>
public class MallStockLog : BaseEntity
{
    /// <summary>
    /// 关联规格。
    /// </summary>
    public Guid SkuId { get; private set; }

    /// <summary>
    /// 规格编码快照。
    /// </summary>
    public string SkuCode { get; private set; } = string.Empty;

    /// <summary>
    /// 规格名称快照。
    /// </summary>
    public string SpecName { get; private set; } = string.Empty;

    /// <summary>
    /// 变更类型，见 <see cref="MallStockChangeType"/>。
    /// </summary>
    public string ChangeType { get; private set; } = string.Empty;

    /// <summary>
    /// 变更数量，正数为增加，负数为扣减。
    /// </summary>
    public int ChangeQuantity { get; private set; }

    /// <summary>
    /// 变更前库存。
    /// </summary>
    public int BeforeStock { get; private set; }

    /// <summary>
    /// 变更后库存。
    /// </summary>
    public int AfterStock { get; private set; }

    /// <summary>
    /// 关联订单号，订单链路产生时记录。
    /// </summary>
    public string? RelatedOrderNo { get; private set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; private set; }

    private MallStockLog()
    {
    }

    private MallStockLog(
        Guid skuId,
        string skuCode,
        string specName,
        string changeType,
        int beforeStock,
        int afterStock,
        string? relatedOrderNo,
        string? remark)
    {
        SkuId = skuId;
        SkuCode = skuCode;
        SpecName = specName;
        ChangeType = changeType;
        BeforeStock = beforeStock;
        AfterStock = afterStock;
        ChangeQuantity = afterStock - beforeStock;
        RelatedOrderNo = relatedOrderNo;
        Remark = remark;
    }

    public static MallStockLog Create(
        Guid skuId,
        string skuCode,
        string specName,
        string changeType,
        int beforeStock,
        int afterStock,
        string? relatedOrderNo = null,
        string? remark = null)
    {
        return new MallStockLog(
            skuId,
            skuCode,
            specName,
            changeType,
            beforeStock,
            afterStock,
            relatedOrderNo,
            remark);
    }
}
