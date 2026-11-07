using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 运单，一单一包裹，人工维护。
/// </summary>
public class MallShipment : BaseEntity
{
    /// <summary>
    /// 运单号，唯一。
    /// </summary>
    public string ShipmentNo { get; private set; } = string.Empty;

    /// <summary>
    /// 所属订单，一单一运单。
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// 快递公司。
    /// </summary>
    public string ExpressCompany { get; private set; } = string.Empty;

    /// <summary>
    /// 快递单号，唯一。
    /// </summary>
    public string TrackingNo { get; private set; } = string.Empty;

    /// <summary>
    /// 运单状态，取值见 <see cref="MallShipmentStatus"/>。
    /// </summary>
    public string Status { get; private set; } = MallShipmentStatus.Shipped;

    /// <summary>
    /// 发货时间。
    /// </summary>
    public DateTime? ShippedAt { get; private set; }

    /// <summary>
    /// 签收时间。
    /// </summary>
    public DateTime? DeliveredAt { get; private set; }

    /// <summary>
    /// 运单备注。
    /// </summary>
    public string Remark { get; private set; } = string.Empty;

    /// <summary>
    /// 是否已作废：作废后运单号释放，可被其他订单使用，记录保留可查。
    /// </summary>
    public bool IsVoided { get; private set; }

    /// <summary>
    /// 作废时间。
    /// </summary>
    public DateTime? VoidedAt { get; private set; }

    /// <summary>
    /// 作废操作人。
    /// </summary>
    public string VoidedBy { get; private set; } = string.Empty;

    /// <summary>
    /// 作废原因。
    /// </summary>
    public string VoidReason { get; private set; } = string.Empty;

    private MallShipment()
    {
    }

    private MallShipment(
        string shipmentNo,
        Guid orderId,
        string expressCompany,
        string trackingNo,
        string remark)
    {
        ShipmentNo = shipmentNo;
        OrderId = orderId;
        ExpressCompany = expressCompany;
        TrackingNo = trackingNo;
        Remark = remark;
        Status = MallShipmentStatus.Shipped;
        ShippedAt = DateTime.Now;
    }

    public static MallShipment Create(
        string shipmentNo,
        Guid orderId,
        string expressCompany,
        string trackingNo,
        string remark)
    {
        return new MallShipment(shipmentNo, orderId, expressCompany, trackingNo, remark);
    }

    /// <summary>
    /// 标记签收。
    /// </summary>
    public void MarkDelivered()
    {
        if (IsVoided)
        {
            throw new BusinessException(400, "运单已作废，不可签收");
        }

        if (Status != MallShipmentStatus.Shipped)
        {
            throw new BusinessException(400, "当前运单状态不可签收");
        }

        Status = MallShipmentStatus.Delivered;
        DeliveredAt = DateTime.Now;
    }

    /// <summary>
    /// 作废运单：保留记录用于追溯，运单号随作废释放。
    /// </summary>
    public void Void(string reason, string operatorName)
    {
        if (IsVoided)
        {
            throw new BusinessException(400, "该运单已作废");
        }

        var normalizedReason = reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedReason))
        {
            throw new BusinessException(400, "请填写作废原因");
        }

        IsVoided = true;
        VoidedAt = DateTime.Now;
        VoidedBy = operatorName?.Trim() ?? string.Empty;
        VoidReason = normalizedReason;
    }
}
