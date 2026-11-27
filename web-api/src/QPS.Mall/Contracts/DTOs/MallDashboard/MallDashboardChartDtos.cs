namespace QPS.Mall.Contracts.DTOs.MallDashboard;

/// <summary>
/// 订单趋势：近 7 天每日下单量（含所有状态，按 CreatedAt 归属日期）。
/// </summary>
public class MallOrderTrendItemDto
{
    public DateTime Date { get; set; }
    public int OrderCount { get; set; }
}

/// <summary>
/// 销售额趋势：近 7 天每日实收金额（按 PaidAt 归属日期，仅成交状态）。
/// </summary>
public class MallSalesTrendItemDto
{
    public DateTime Date { get; set; }
    public decimal SalesAmount { get; set; }
}

/// <summary>
/// 订单状态分布：Code 为后端状态值，Name 为中文名，Value 为订单数。
/// </summary>
public class MallOrderStatusItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

/// <summary>
/// 热销商品：按 ProductId + ProductName 聚合的销售件数与销售额。
/// </summary>
public class MallTopProductItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal SalesAmount { get; set; }
}

/// <summary>
/// 核心指标概览：近 7 天经营汇总。
/// 支付转化率 = 成交订单数 / 订单总数；客单价 = 销售额 / 成交订单数。
/// </summary>
public class MallDashboardSummaryDto
{
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int ItemsSold { get; set; }
    public decimal RefundAmount { get; set; }
    public int PendingShipment { get; set; }
}

/// <summary>
/// 客单价趋势：近 7 天每日实收金额 / 当日成交订单数。
/// </summary>
public class MallAovTrendItemDto
{
    public DateTime Date { get; set; }
    public decimal AverageOrderValue { get; set; }
}

/// <summary>
/// 转化漏斗：同一批下单订单（近 7 天 CreatedAt）的支付、发货、完成阶段计数。
/// </summary>
public class MallConversionFunnelItemDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
