using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 商城订单。
/// </summary>
public class MallOrder : BaseEntity
{
    /// <summary>
    /// 订单号，唯一。
    /// </summary>
    public string OrderNo { get; private set; } = string.Empty;

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
    /// 商品原价合计（促销与优惠券抵扣前）。
    /// </summary>
    public decimal GoodsAmount { get; private set; }

    /// <summary>
    /// 促销优惠金额（下单时快照，含折扣与满减）。
    /// </summary>
    public decimal PromotionDiscountAmount { get; private set; }

    /// <summary>
    /// 优惠券抵扣金额。
    /// </summary>
    public decimal CouponDiscountAmount { get; private set; }

    /// <summary>
    /// 优惠券 ID 快照（券后续被删除仍可追溯）。
    /// </summary>
    public Guid? CouponId { get; private set; }

    /// <summary>
    /// 优惠券名称快照。
    /// </summary>
    public string CouponName { get; private set; } = string.Empty;

    /// <summary>
    /// 运费金额。
    /// </summary>
    public decimal FreightAmount { get; private set; }

    /// <summary>
    /// 应付金额 = max(0, 商品原价 - 促销优惠 - 券抵扣) + 运费金额。
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
    /// 赠品未发放原因（库存不足等），下单时写入，用于订单详情追溯。
    /// </summary>
    public string GiftShortageMessage { get; private set; } = string.Empty;

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
        Guid? memberId,
        string receiverName,
        string receiverPhone,
        string receiverAddress,
        decimal goodsAmount,
        decimal promotionDiscountAmount,
        decimal couponDiscountAmount,
        decimal freightAmount,
        int totalWeightGram,
        string remark,
        Guid? couponId,
        string couponName)
    {
        OrderNo = orderNo;
        MemberId = memberId;
        ReceiverName = receiverName;
        ReceiverPhone = receiverPhone;
        ReceiverAddress = receiverAddress;
        GoodsAmount = goodsAmount;
        PromotionDiscountAmount = promotionDiscountAmount;
        CouponDiscountAmount = couponDiscountAmount;
        CouponId = couponId;
        CouponName = couponName;
        FreightAmount = freightAmount;
        PayableAmount = CalculatePayableAmount(goodsAmount, promotionDiscountAmount, couponDiscountAmount, freightAmount);
        TotalWeightGram = totalWeightGram;
        Remark = remark;
        Status = MallOrderStatus.PendingPayment;
    }

    public static MallOrder Create(
        string orderNo,
        Guid? memberId,
        string receiverName,
        string receiverPhone,
        string receiverAddress,
        decimal goodsAmount,
        decimal promotionDiscountAmount,
        decimal couponDiscountAmount,
        decimal freightAmount,
        int totalWeightGram,
        string remark,
        Guid? couponId = null,
        string couponName = "")
    {
        if (goodsAmount < 0 || promotionDiscountAmount < 0 || couponDiscountAmount < 0 || freightAmount < 0)
        {
            throw new BusinessException(400, "订单金额不能为负数");
        }

        return new MallOrder(
            orderNo,
            memberId,
            receiverName,
            receiverPhone,
            receiverAddress,
            goodsAmount,
            promotionDiscountAmount,
            couponDiscountAmount,
            freightAmount,
            totalWeightGram,
            remark,
            couponId,
            couponName);
    }

    /// <summary>
    /// 应付金额唯一口径：商品原价扣除促销与券后封底为 0，再加运费。
    /// </summary>
    private static decimal CalculatePayableAmount(
        decimal goodsAmount,
        decimal promotionDiscountAmount,
        decimal couponDiscountAmount,
        decimal freightAmount)
    {
        return Math.Max(0, goodsAmount - promotionDiscountAmount - couponDiscountAmount) + freightAmount;
    }

    /// <summary>
    /// 金额恒等式校验：商品原价 − 促销优惠 − 券抵扣（封底为 0）+ 运费 = 应付金额。
    /// 下单时与订单查询时共用，口径不一致直接暴露错误，不静默展示。
    /// </summary>
    public static void EnsureAmountIdentity(
        string orderNo,
        decimal goodsAmount,
        decimal promotionDiscountAmount,
        decimal couponDiscountAmount,
        decimal freightAmount,
        decimal payableAmount)
    {
        var expected = CalculatePayableAmount(
            goodsAmount,
            promotionDiscountAmount,
            couponDiscountAmount,
            freightAmount);
        if (Math.Abs(expected - payableAmount) > 0.01m)
        {
            throw new BusinessException(
                500,
                $"订单 {orderNo} 金额不自洽：商品 {goodsAmount:0.00} - 促销 {promotionDiscountAmount:0.00} - 券 {couponDiscountAmount:0.00} + 运费 {freightAmount:0.00} = {expected:0.00}，实际应付 {payableAmount:0.00}");
        }
    }

    /// <summary>
    /// 记录赠品未发放原因（不拦截下单，仅用于订单追溯）。
    /// </summary>
    public void RecordGiftShortage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        GiftShortageMessage = string.IsNullOrEmpty(GiftShortageMessage)
            ? message
            : $"{GiftShortageMessage}；{message}";
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
    /// 运单作废后订单回退到已支付（待发货），可重新发货。
    /// </summary>
    public void RevertShipment()
    {
        if (Status != MallOrderStatus.Shipped)
        {
            throw new BusinessException(400, "仅已发货订单可以作废运单");
        }

        Status = MallOrderStatus.Paid;
        ShippedAt = null;
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
    /// 进入退款流程。已支付（未发货）、已发货、已签收订单均可发起整单退款；
    /// 已发货/已签收订单退款不立即回补库存，等待退货入库确认。
    /// </summary>
    public void BeginRefund()
    {
        if (Status != MallOrderStatus.Paid &&
            Status != MallOrderStatus.Shipped &&
            Status != MallOrderStatus.Completed)
        {
            throw new BusinessException(400, "当前订单状态不可申请退款");
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
        FailRefund(MallOrderStatus.Paid);
    }

    /// <summary>
    /// 退款失败，订单回到退款前状态。
    /// 未发货回到已支付；已发货回到已发货；已签收回已完成，避免丢失发货状态。
    /// </summary>
    public void FailRefund(string previousStatus)
    {
        if (Status != MallOrderStatus.Refunding)
        {
            throw new BusinessException(400, "当前订单不在退款流程中");
        }

        Status = previousStatus;
    }

    /// <summary>
    /// 进入退货流程。已发货/已完成订单可发起；
    /// 已退款但库存尚未回补的订单也可发起退货，等入库确认后再回补。
    /// </summary>
    public void BeginReturn()
    {
        if (Status != MallOrderStatus.Shipped &&
            Status != MallOrderStatus.Completed &&
            Status != MallOrderStatus.Refunded)
        {
            throw new BusinessException(400, "仅已发货、已完成或已退款待入库订单可以申请退货");
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