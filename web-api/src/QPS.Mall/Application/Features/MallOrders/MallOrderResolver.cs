using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallOrders;

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
                .FirstOrDefaultAsync(item => item.Id == orderId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(orderNo))
        {
            var normalized = orderNo.Trim();
            order = await dbContext.MallOrders
                .FirstOrDefaultAsync(item => item.OrderNo == normalized, cancellationToken);
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

    /// <summary>会员归属校验：传入 MemberId 时订单必须属于该会员，管理端不传则跳过。</summary>
    public static void EnsureOwnership(MallOrder order, Guid? memberId)
    {
        if (memberId.HasValue && order.MemberId != memberId.Value)
        {
            throw new BusinessException(403, "无权操作该订单");
        }
    }

    /// <summary>订单是否已经发货（含已签收），用于判断退款时是否立即回补库存。</summary>
    public static bool WasShipped(MallOrder order)
    {
        return order.ShippedAt.HasValue || order.CompletedAt.HasValue;

    }

    /// <summary>退款前状态：已签收 → 已完成，已发货 → 已发货，否则 → 已支付。</summary>
    public static string StatusBeforeRefund(MallOrder order)
    {
        if (order.CompletedAt.HasValue)
        {
            return MallOrderStatus.Completed;
        }

        return order.ShippedAt.HasValue ? MallOrderStatus.Shipped : MallOrderStatus.Paid;
    }

    /// <summary>退货前状态：已退款 → 已退款，已签收 → 已完成，否则 → 已发货。</summary>
    public static string StatusBeforeReturn(MallOrder order)
    {
        if (order.PaidAmount > 0 && order.RefundedAmount >= order.PaidAmount)
        {
            return MallOrderStatus.Refunded;
        }

        return order.CompletedAt.HasValue ? MallOrderStatus.Completed : MallOrderStatus.Shipped;
    }

    /// <summary>是否已回补过库存：以退款/退货回补流水作为幂等标记，不新增表字段。</summary>
    public static async Task<bool> HasRestoredStockAsync(
        IMallDbContext dbContext,
        MallOrder order,
        CancellationToken cancellationToken)
    {
        var pendingLog = dbContext.MallStockLogs.Local.Any(log =>
            log.RelatedOrderNo == order.OrderNo &&
            (log.ChangeType == MallStockChangeType.RefundRestore ||
             log.ChangeType == MallStockChangeType.ReturnRestore));

        if (pendingLog)
        {
            return true;
        }

        return await dbContext.MallStockLogs
            .AsNoTracking()
            .AnyAsync(
                log =>
                    log.RelatedOrderNo == order.OrderNo &&
                    (log.ChangeType == MallStockChangeType.RefundRestore ||
                     log.ChangeType == MallStockChangeType.ReturnRestore),
                cancellationToken);
    }

    /// <summary>
    /// 回补库存且保证只补一次：已存在退款/退货回补流水时跳过。
    /// 调用方必须与状态/退款单变更处于同一事务，流水写入成功才算回补成功。
    /// </summary>
    public static async Task<bool> RestoreStockOnceAsync(
        IMallDbContext dbContext,
        MallOrder order,
        string changeType,
        CancellationToken cancellationToken)
    {
        if (await HasRestoredStockAsync(dbContext, order, cancellationToken))
        {
            return false;
        }

        await RestoreStockAsync(dbContext, order, changeType, cancellationToken);
        return true;
    }

    public static async Task RestoreStockAsync(
        IMallDbContext dbContext,
        MallOrder order,
        string changeType,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.MallOrderItems
            .AsNoTracking()
            .Where(item => item.OrderId == order.Id)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return;
        }

        var skuGroups = items
            .GroupBy(item => item.SkuId)
            .Select(group => new
            {
                SkuId = group.Key,
                Quantity = group.Sum(item => item.Quantity),
                Items = group.ToList()
            })
            .OrderBy(group => group.SkuId)
            .ToList();

        foreach (var group in skuGroups)
        {
            // 原子回补，避免读改写覆盖并发扣减
            var affected = await dbContext.MallSkus
                .Where(sku => sku.Id == group.SkuId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        sku => sku.Stock,
                        sku => sku.Stock + group.Quantity),
                    cancellationToken);

            // 规格已被删除时保持旧行为：跳过该行回补
            if (affected == 0)
            {
                continue;
            }
        }

        // ExecuteUpdateAsync 绕过变更跟踪，回查数据库真实库存后再写流水
        var skuIds = skuGroups.Select(group => group.SkuId).ToList();
        var skuInfos = await dbContext.MallSkus
            .AsNoTracking()
            .Where(sku => skuIds.Contains(sku.Id))
            .Select(sku => new { sku.Id, sku.SkuCode, sku.SpecName, sku.Stock })
            .ToDictionaryAsync(sku => sku.Id, cancellationToken);

        foreach (var group in skuGroups)
        {
            if (!skuInfos.TryGetValue(group.SkuId, out var sku))
            {
                continue;
            }

            // 由回补后的真实库存倒推每行流水的前后值
            var runningAfter = sku.Stock;
            for (var index = group.Items.Count - 1; index >= 0; index--)
            {
                var quantity = group.Items[index].Quantity;
                var afterStock = runningAfter;
                var beforeStock = afterStock - quantity;
                runningAfter = beforeStock;

                dbContext.MallStockLogs.Add(MallStockLog.Create(
                    sku.Id,
                    sku.SkuCode,
                    sku.SpecName,
                    changeType,
                    beforeStock,
                    afterStock,
                    relatedOrderNo: order.OrderNo));
            }
        }
    }
}