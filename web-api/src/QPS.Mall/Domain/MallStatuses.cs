namespace QPS.Mall.Domain;

/// <summary>
/// 商场订单状态。
/// </summary>
public static class MallOrderStatus
{
    /// <summary>待支付。</summary>
    public const string PendingPayment = "PendingPayment";

    /// <summary>已支付。</summary>
    public const string Paid = "Paid";

    /// <summary>已发货。</summary>
    public const string Shipped = "Shipped";

    /// <summary>已完成。</summary>
    public const string Completed = "Completed";

    /// <summary>已取消。</summary>
    public const string Cancelled = "Cancelled";

    /// <summary>退款中。</summary>
    public const string Refunding = "Refunding";

    /// <summary>已退款。</summary>
    public const string Refunded = "Refunded";

    /// <summary>退货中。</summary>
    public const string Returning = "Returning";

    /// <summary>已退货。</summary>
    public const string Returned = "Returned";
}

/// <summary>
/// 商场支付单状态。
/// </summary>
public static class MallPaymentStatus
{
    /// <summary>待支付。</summary>
    public const string Pending = "Pending";

    /// <summary>支付成功。</summary>
    public const string Success = "Success";

    /// <summary>支付失败。</summary>
    public const string Failed = "Failed";

    /// <summary>已关闭（订单取消）。</summary>
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// 商场退款单状态。
/// </summary>
public static class MallRefundStatus
{
    /// <summary>退款处理中。</summary>
    public const string Pending = "Pending";

    /// <summary>退款成功。</summary>
    public const string Success = "Success";

    /// <summary>退款失败。</summary>
    public const string Failed = "Failed";
}

/// <summary>
/// 商场退货单状态。
/// </summary>
public static class MallReturnStatus
{
    /// <summary>待审核。</summary>
    public const string Pending = "Pending";

    /// <summary>审核通过，等待商家收到退货。</summary>
    public const string Approved = "Approved";

    /// <summary>商家已收到退货。</summary>
    public const string Received = "Received";

    /// <summary>退货完成（退款成功）。</summary>
    public const string Completed = "Completed";

    /// <summary>审核拒绝。</summary>
    public const string Rejected = "Rejected";
}

/// <summary>
/// 商场库存变更类型。
/// </summary>
public static class MallStockChangeType
{
    /// <summary>下单扣减。</summary>
    public const string OrderDeduct = "OrderDeduct";

    /// <summary>取消订单回补。</summary>
    public const string OrderCancel = "OrderCancel";

    /// <summary>退款成功回补。</summary>
    public const string RefundRestore = "RefundRestore";

    /// <summary>退货收货回补。</summary>
    public const string ReturnRestore = "ReturnRestore";

    /// <summary>手动调整。</summary>
    public const string ManualAdjust = "ManualAdjust";
}

/// <summary>
/// 商场运单状态。
/// </summary>
public static class MallShipmentStatus
{
    /// <summary>已发货。</summary>
    public const string Shipped = "Shipped";

    /// <summary>已签收。</summary>
    public const string Delivered = "Delivered";
}