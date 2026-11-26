using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs.MallDashboard;
using QPS.Mall.Contracts.Permissions;
using QPS.Mall.Domain;
using QPS.Shared.Contracts;

namespace QPS.Mall.Application.Features.MallDashboard;

/// <summary>
/// 商城数据看板图表类型。
/// </summary>
public enum MallDashboardChart
{
    OrderTrend,
    SalesTrend,
    OrderStatusDistribution,
    TopProducts,
    Summary,
    AovTrend,
    ConversionFunnel
}

/// <summary>
/// 商城数据看板：固定近 7 天窗口。权限码标在 Query 上，供权限管道与架构护栏识别。
/// </summary>
[Permission(MallPermissionCodes.MALL_DASHBOARD_VIEW)]
public sealed record GetMallDashboardChartQuery(MallDashboardChart Chart) : IRequest<object>;

public class GetMallDashboardChartHandler : IRequestHandler<GetMallDashboardChartQuery, object>
{
    private static readonly string[] DealStatuses =
    [
        MallOrderStatus.Paid,
        MallOrderStatus.Shipped,
        MallOrderStatus.Completed
    ];

    private static readonly Dictionary<string, string> StatusNames = new()
    {
        [MallOrderStatus.PendingPayment] = "待支付",
        [MallOrderStatus.Paid] = "已支付",
        [MallOrderStatus.Shipped] = "已发货",
        [MallOrderStatus.Completed] = "已完成",
        [MallOrderStatus.Cancelled] = "已取消",
        [MallOrderStatus.Refunding] = "退款中",
        [MallOrderStatus.Refunded] = "已退款",
        [MallOrderStatus.Returning] = "退货中",
        [MallOrderStatus.Returned] = "已退货"
    };

    private readonly IMallDbContext _dbContext;

    public GetMallDashboardChartHandler(IMallDbContext dbContext) => _dbContext = dbContext;

    public async Task<object> Handle(GetMallDashboardChartQuery request, CancellationToken cancellationToken) => request.Chart switch
    {
        MallDashboardChart.OrderTrend => await GetOrderTrendAsync(cancellationToken),
        MallDashboardChart.SalesTrend => await GetSalesTrendAsync(cancellationToken),
        MallDashboardChart.OrderStatusDistribution => await GetOrderStatusDistributionAsync(cancellationToken),
        MallDashboardChart.TopProducts => await GetTopProductsAsync(cancellationToken),
        MallDashboardChart.Summary => await GetSummaryAsync(cancellationToken),
        MallDashboardChart.AovTrend => await GetAovTrendAsync(cancellationToken),
        MallDashboardChart.ConversionFunnel => await GetConversionFunnelAsync(cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(request.Chart), request.Chart, null)
    };

    private async Task<List<MallOrderTrendItemDto>> GetOrderTrendAsync(CancellationToken cancellationToken)
    {
        var (start, end) = GetWindow();
        var createdDates = await _dbContext.MallOrders
            .Where(order => order.CreatedAt >= start && order.CreatedAt < end)
            .Select(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        return BuildDays(start).Select(date => new MallOrderTrendItemDto
        {
            Date = date,
            OrderCount = createdDates.Count(createdAt => createdAt >= date && createdAt < date.AddDays(1))
        }).ToList();
    }

    private async Task<List<MallSalesTrendItemDto>> GetSalesTrendAsync(CancellationToken cancellationToken)
    {
        var (start, end) = GetWindow();
        var paidOrders = await _dbContext.MallOrders
            .Where(order => DealStatuses.Contains(order.Status)
                            && order.PaidAt.HasValue
                            && order.PaidAt.Value >= start
                            && order.PaidAt.Value < end)
            .Select(order => new { order.PaidAt, order.PaidAmount })
            .ToListAsync(cancellationToken);

        return BuildDays(start).Select(date => new MallSalesTrendItemDto
        {
            Date = date,
            SalesAmount = Math.Round(
                paidOrders.Where(order => order.PaidAt!.Value >= date && order.PaidAt.Value < date.AddDays(1))
                    .Sum(order => order.PaidAmount),
                2,
                MidpointRounding.AwayFromZero)
        }).ToList();
    }

    private async Task<List<MallOrderStatusItemDto>> GetOrderStatusDistributionAsync(CancellationToken cancellationToken)
    {
        var (start, end) = GetWindow();
        var statuses = await _dbContext.MallOrders
            .Where(order => order.CreatedAt >= start && order.CreatedAt < end)
            .Select(order => order.Status)
            .ToListAsync(cancellationToken);

        return statuses
            .GroupBy(status => status)
            .Select(group => new MallOrderStatusItemDto
            {
                Code = group.Key,
                Name = StatusNames.TryGetValue(group.Key, out var name) ? name : group.Key,
                Value = group.Count()
            })
            .Where(item => item.Value > 0)
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Name)
            .ToList();
    }

    private async Task<List<MallTopProductItemDto>> GetTopProductsAsync(CancellationToken cancellationToken)
    {
        var (start, end) = GetWindow();
        var rows = await (from item in _dbContext.MallOrderItems
                          join order in _dbContext.MallOrders on item.OrderId equals order.Id
                          where order.CreatedAt >= start && order.CreatedAt < end
                                && DealStatuses.Contains(order.Status)
                                && !item.IsGift
                          select new
                          {
                              item.ProductId,
                              item.ProductName,
                              item.Quantity,
                              item.ItemAmount
                          }).ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => new { row.ProductId, row.ProductName })
            .Select(group => new MallTopProductItemDto
            {
                ProductId = group.Key.ProductId,
                ProductName = group.Key.ProductName,
                Quantity = group.Sum(row => row.Quantity),
                SalesAmount = Math.Round(group.Sum(row => row.ItemAmount), 2, MidpointRounding.AwayFromZero)
            })
            .OrderByDescending(item => item.Quantity)
            .ThenByDescending(item => item.SalesAmount)
            .ThenBy(item => item.ProductName)
            .Take(10)
            .ToList();
    }

    private async Task<MallDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var (start, end) = GetWindow();

        // 下单数与待发货：按 CreatedAt 归属窗口，状态取当前值
        var orderRows = await _dbContext.MallOrders
            .Where(order => order.CreatedAt >= start && order.CreatedAt < end)
            .Select(order => new { order.Status })
            .ToListAsync(cancellationToken);
        var totalOrders = orderRows.Count;
        var pendingShipment = orderRows.Count(row => row.Status == MallOrderStatus.Paid);

        // 成交订单与销售额：按 PaidAt 归属窗口，仅成交状态
        var paidRows = await _dbContext.MallOrders
            .Where(order => DealStatuses.Contains(order.Status)
                            && order.PaidAt.HasValue
                            && order.PaidAt.Value >= start
                            && order.PaidAt.Value < end)
            .Select(order => new { order.PaidAmount })
            .ToListAsync(cancellationToken);
        var paidOrders = paidRows.Count;
        var salesAmount = paidRows.Sum(row => row.PaidAmount);

        // 商品销量：近 7 天成交订单的非赠品明细
        var itemsSold = await (from item in _dbContext.MallOrderItems
                               join order in _dbContext.MallOrders on item.OrderId equals order.Id
                               where order.CreatedAt >= start && order.CreatedAt < end
                                     && DealStatuses.Contains(order.Status)
                                     && !item.IsGift
                               select item.Quantity)
            .SumAsync(cancellationToken);

        // 退款金额：按 RefundedAt 归属窗口
        var refundAmount = await _dbContext.MallOrders
            .Where(order => order.RefundedAt.HasValue
                            && order.RefundedAt.Value >= start
                            && order.RefundedAt.Value < end)
            .SumAsync(order => order.RefundedAmount, cancellationToken);

        return new MallDashboardSummaryDto
        {
            TotalOrders = totalOrders,
            PaidOrders = paidOrders,
            ConversionRate = totalOrders == 0
                ? 0
                : Math.Round((decimal)paidOrders / totalOrders * 100, 2, MidpointRounding.AwayFromZero),
            SalesAmount = Math.Round(salesAmount, 2, MidpointRounding.AwayFromZero),
            AverageOrderValue = paidOrders == 0
                ? 0
                : Math.Round(salesAmount / paidOrders, 2, MidpointRounding.AwayFromZero),
            ItemsSold = itemsSold,
            RefundAmount = Math.Round(refundAmount, 2, MidpointRounding.AwayFromZero),
            PendingShipment = pendingShipment
        };
    }

    private async Task<List<MallAovTrendItemDto>> GetAovTrendAsync(CancellationToken cancellationToken)
    {
        var (start, end) = GetWindow();
        var paidOrders = await _dbContext.MallOrders
            .Where(order => DealStatuses.Contains(order.Status)
                            && order.PaidAt.HasValue
                            && order.PaidAt.Value >= start
                            && order.PaidAt.Value < end)
            .Select(order => new { order.PaidAt, order.PaidAmount })
            .ToListAsync(cancellationToken);

        return BuildDays(start).Select(date =>
        {
            var dayOrders = paidOrders
                .Where(order => order.PaidAt!.Value >= date && order.PaidAt.Value < date.AddDays(1))
                .ToList();
            return new MallAovTrendItemDto
            {
                Date = date,
                AverageOrderValue = dayOrders.Count == 0
                    ? 0
                    : Math.Round(dayOrders.Sum(order => order.PaidAmount) / dayOrders.Count, 2,
                        MidpointRounding.AwayFromZero)
            };
        }).ToList();
    }

    private async Task<List<MallConversionFunnelItemDto>> GetConversionFunnelAsync(CancellationToken cancellationToken)
    {
        var (start, end) = GetWindow();
        // 同一批下单订单（CreatedAt 归属窗口），按各阶段时间是否发生统计，口径自上而下包含
        var orders = await _dbContext.MallOrders
            .Where(order => order.CreatedAt >= start && order.CreatedAt < end)
            .Select(order => new { order.PaidAt, order.ShippedAt, order.CompletedAt })
            .ToListAsync(cancellationToken);

        return
        [
            new MallConversionFunnelItemDto { Name = "下单", Value = orders.Count },
            new MallConversionFunnelItemDto { Name = "已支付", Value = orders.Count(order => order.PaidAt.HasValue) },
            new MallConversionFunnelItemDto { Name = "已发货", Value = orders.Count(order => order.ShippedAt.HasValue) },
            new MallConversionFunnelItemDto { Name = "已完成", Value = orders.Count(order => order.CompletedAt.HasValue) }
        ];
    }

    private static (DateTime Start, DateTime End) GetWindow()
    {
        var today = DateTime.Today;
        return (today.AddDays(-6), today.AddDays(1));
    }

    private static IEnumerable<DateTime> BuildDays(DateTime start) =>
        Enumerable.Range(0, 7).Select(offset => start.AddDays(offset));
}
