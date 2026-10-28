using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 商场订单。
/// </summary>
public class MallOrder : BaseEntity
{
    /// <summary>
    /// 订单号，唯一。
    /// </summary>
    public string OrderNo { get; private set; } = string.Empty;

    /// <summary>
    /// 所属门店。
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// 下单会员ID，关联 MallMember。后台代下单可为空。
    /// </summary>
    public Guid? MemberId { get; private set; }

    /// <summary>
    /// 订单状态，取值见 <see cref="MallOrderStatus"/>。
    /// </summary>
    public string Status { get; private set; } = MallOrderStatus.PendingPayment;

    /// <summary>
    /// 收货人姓名。
    /// </summary>
    public string ReceiverName { get; private set; } = string.Empty;

    /// <summary>
    /// 收货人电话。
    /// </summary>
    public string ReceiverPhone { get; private set; } = string.Empty;

    /// <summary>
    /// 收货地址。
    /// </summary>
    public string ReceiverAddress { get; private set; } = string.Empty;

    /// <summary>
    /// 收货地址ID，关联 MallAddress。
    /// </summary>
    public Guid? AddressId { get; private set; }

    /// <summary>
    /// 商品金额。
    /// </summary>
    public decimal GoodsAmount { get; private set; }

    /// <summary>
    /// 运费金额。
    /// </summary>
    public decimal FreightAmount { get; private set; }

    /// <summary>
    /// 应付金额 = 商品金额 + 运费金额。
    /// </summary>
    public decimal PayableAmount { get; private set; }

    /// <summary>
    /// 已支付金额，仅累计支付成功的单据。
    /// </summary>
    public decimal PaidAmount { get; private set; }

    /// <summary>
    /// 已退款金额，仅累计退款成功的单据。
    /// </summary>
    public decimal RefundedAmount { get; private set; }

    /// <summary>
    /// 订单总重量（克）。
    /// </summary>
    public int TotalWeightGram { get; private set; }

    /// <summary>
    /// 订单备注。
    /// </summary>
    public string Remark { get; private set; } = string.Empty;

    /// <summary>
    /// 支付时间。
    /// </summary>
    public DateTime? PaidAt { get; private set; }

    /// <summary>
    /// 发货时间。
    /// </summary>
    public DateTime? ShippedAt { get; private set; }

    /// <summary>
    /// 完成时间。
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// 取消时间。
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// 退款完成时间。
    /// </summary>
    public DateTime? RefundedAt { get; private set; }

    private MallOrder()
    {
    }

    private MallOrder(
        string orderNo,
        Guid storeId,
        Guid? memberId,
        string receiverName,
        string receiverPhone,
        string receiverAddress,
        decimal goodsAmount,
        decimal freightAmount,
        int totalWeightGram,
        string remark,
        Guid? addressId)
    {
        OrderNo = orderNo;
        StoreId = storeId;
        MemberId = memberId;
        ReceiverName = receiverName;
        ReceiverPhone = receiverPhone;
        ReceiverAddress = receiverAddress;
        GoodsAmount = goodsAmount;
        FreightAmount = freightAmount;
        PayableAmount = goodsAmount + freightAmount;
        TotalWeightGram = totalWeightGram;
        Remark = remark;
        AddressId = addressId;
        Status = MallOrderStatus.PendingPayment;
    }

    public static MallOrder Create(
        string orderNo,
        Guid storeId,
        Guid? memberId,
        string receiverName,
        string receiverPhone,
        string receiverAddress,
        decimal goodsAmount,
        decimal freightAmount,
        int totalWeightGram,
        string remark,
        Guid? addressId = null)
    {
        return new MallOrder(
            orderNo,
            storeId,
            memberId,
            receiverName,
            receiverPhone,
            receiverAddress,
            goodsAmount,
            freightAmount,
            totalWeightGram,
            remark,
            addressId);
    }

    /// <summary>
    /// 标记支付成功。
    /// </summary>
    public void MarkPaid(decimal amount)
    {
        if (Status != MallOrderStatus.PendingPayment)
        {
            throw new BusinessException(400, "当前订单状态不可支付");
        }

        Status = MallOrderStatus.Paid;
        PaidAmount = amount;
        PaidAt = DateTime.Now;
    }

    /// <summary>
    /// 取消待支付订单。
    /// </summary>
    public void Cancel()
    {
        if (Status != MallOrderStatus.PendingPayment)
        {
            throw new BusinessException(400, "仅待支付订单可以取消");
        }

        Status = MallOrderStatus.Cancelled;
        CancelledAt = DateTime.Now;
    }

    /// <summary>
    /// 标记发货。
    /// </summary>
    public void MarkShipped()
    {
        if (Status != MallOrderStatus.Paid)
        {
            throw new BusinessException(400, "仅已支付订单可以发货");
        }

        Status = MallOrderStatus.Shipped;
        ShippedAt = DateTime.Now;
    }

    /// <summary>
    /// 确认收货。
    /// </summary>
    public void MarkCompleted()
    {
        if (Status != MallOrderStatus.Shipped)
        {
            throw new BusinessException(400, "仅已发货订单可以确认收货");
        }

        Status = MallOrderStatus.Completed;
        CompletedAt = DateTime.Now;
    }

    /// <summary>
    /// 进入退款流程。
    /// </summary>
    public void BeginRefund()
    {
        if (Status != MallOrderStatus.Paid)
        {
            throw new BusinessException(400, "仅已支付未发货订单可以申请退款");
        }

        Status = MallOrderStatus.Refunding;
    }

    /// <summary>
    /// 退款成功。
    /// </summary>
    public void CompleteRefund()
    {
        if (Status != MallOrderStatus.Refunding)
        {
            throw new BusinessException(400, "当前订单不在退款流程中");
        }

        Status = MallOrderStatus.Refunded;
        RefundedAmount = PaidAmount;
        RefundedAt = DateTime.Now;
    }

    /// <summary>
    /// 退款失败，订单回到已支付。
    /// </summary>
    public void FailRefund()
    {
        if (Status != MallOrderStatus.Refunding)
        {
            throw new BusinessException(400, "当前订单不在退款流程中");
        }

        Status = MallOrderStatus.Paid;
    }

    /// <summary>
    /// 进入退货流程，已发货/已完成订单可发起。
    /// </summary>
    public void BeginReturn()
    {
        if (Status != MallOrderStatus.Shipped && Status != MallOrderStatus.Completed)
        {
            throw new BusinessException(400, "仅已发货或已完成订单可以申请退货");
        }

        Status = MallOrderStatus.Returning;
    }

    /// <summary>
    /// 退货完成（收到退货并退款成功）。
    /// </summary>
    public void CompleteReturn()
    {
        if (Status != MallOrderStatus.Returning)
        {
            throw new BusinessException(400, "当前订单不在退货流程中");
        }

        Status = MallOrderStatus.Returned;
        RefundedAmount = PaidAmount;
        RefundedAt = DateTime.Now;
    }

    /// <summary>
    /// 退货申请被拒绝，订单回到原状态。
    /// </summary>
    public void FailReturn(string previousStatus)
    {
        if (Status != MallOrderStatus.Returning)
        {
            throw new BusinessException(400, "当前订单不在退货流程中");
        }

        Status = previousStatus;
    }
}