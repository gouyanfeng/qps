using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 订单定位：管理端使用 Id，外部接口使用订单号。
/// </summary>
internal static class MallOrderResolver
{
    public static async Task<MallOrder> ResolveAsync(
        IMallDbContext dbContext,
        Guid? orderId,
        string? orderNo,
        CancellationToken cancellationToken)
    {
        MallOrder? order;

        if (orderId.HasValue)
        {
            order = await dbContext.MallOrders
                .FirstOrDefaultAsync(item => item.Id == orderId.Value && !item.IsDeleted, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(orderNo))
        {
            var normalized = orderNo.Trim();
            order = await dbContext.MallOrders
                .FirstOrDefaultAsync(item => item.OrderNo == normalized && !item.IsDeleted, cancellationToken);
        }
        else
        {
            throw new BusinessException(400, "订单标识不能为空");
        }

        if (order is null)
        {
            throw new BusinessException(404, "订单不存在");
        }

        return order;
    }

    /// <summary>
    /// 回补订单明细对应的 SKU 库存并记录流水，用于取消与退款。
    /// </summary>
    public static async Task RestoreStockAsync(
        IMallDbContext dbContext,
        MallOrder order,
        string changeType,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.MallOrderItems
            .AsNoTracking()
            .Where(item => item.OrderId == order.Id && !item.IsDeleted)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return;
        }

        var skuIds = items.Select(item => item.SkuId).Distinct().ToList();

        var skus = await dbContext.MallSkus
            .Where(sku => skuIds.Contains(sku.Id) && !sku.IsDeleted)
            .ToListAsync(cancellationToken);

        var skuMap = skus.ToDictionary(sku => sku.Id);

        foreach (var item in items)
        {
            if (!skuMap.TryGetValue(item.SkuId, out var sku))
            {
                continue;
            }

            var beforeStock = sku.RestoreStock(item.Quantity);

            dbContext.MallStockLogs.Add(MallStockLog.Create(
                sku.Id,
                sku.SkuCode,
                sku.SpecName,
                changeType,
                beforeStock,
                sku.Stock,
                relatedOrderNo: order.OrderNo));
        }
    }
}
