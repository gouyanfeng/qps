using System.Text.Json;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 订单快照构建：订单头、商品行与赠品行的快照字段全部集中在此，
/// 下单流程只提供业务数据，新增快照字段只改这一处。
/// </summary>
public static class OrderSnapshotBuilder
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>构建订单头快照，并在落库前校验金额恒等式。</summary>
    public static MallOrder BuildOrder(OrderHeaderSnapshot snapshot)
    {
        var order = MallOrder.Create(
            snapshot.OrderNo,
            snapshot.MemberId,
            snapshot.ReceiverName,
            snapshot.ReceiverPhone,
            snapshot.ReceiverAddress,
            snapshot.GoodsAmount,
            snapshot.PromotionDiscountAmount,
            snapshot.CouponDiscountAmount,
            snapshot.FreightAmount,
            snapshot.TotalWeightGram,
            snapshot.Remark,
            snapshot.CouponId,
            snapshot.CouponName);

        MallOrder.EnsureAmountIdentity(
            order.OrderNo,
            order.GoodsAmount,
            order.PromotionDiscountAmount,
            order.CouponDiscountAmount,
            order.FreightAmount,
            order.PayableAmount);

        return order;
    }

    /// <summary>构建正常商品行快照：成交价、原价（恒有值）、本行优惠额与结构化促销快照。</summary>
    public static MallOrderItem BuildProductItem(
        MallOrder order,
        MallSku sku,
        MallProduct product,
        SkuOrderRow appliedItem)
    {
        var promotions = appliedItem.AppliedPromotions
            .Where(record => record.DiscountAmount > 0)
            .OrderByDescending(record => record.DiscountAmount)
            .ToList();

        // 主促销只存名称（不拼接金额），多促销明细走 PromotionSnapshot 由展示层拼装
        var primary = promotions.FirstOrDefault();

        return MallOrderItem.Create(
            order.Id,
            sku.Id,
            product.Id,
            product.Name,
            sku.SkuCode,
            sku.SpecName,
            appliedItem.CurrentPrice,
            appliedItem.Quantity,
            sku.WeightGram,
            isGift: false,
            promotionId: primary?.PromotionId,
            promotionName: primary?.PromotionName ?? string.Empty,
            originalUnitPrice: appliedItem.OriginalPrice,
            productImage: product.MainImageUrl,
            promotionDiscountAmount: promotions.Sum(record => record.DiscountAmount),
            promotionSnapshot: BuildPromotionSnapshot(promotions));
    }

    /// <summary>构建赠品行快照：0 元计价，促销快照记录买赠来源（金额 0）。</summary>
    public static MallOrderItem BuildGiftItem(
        MallOrder order,
        MallSku sku,
        MallProduct product,
        GiftSkuRow gift)
    {
        var record = new AppliedPromotionRecord
        {
            PromotionId = gift.PromotionId,
            PromotionName = gift.PromotionName,
            ActionType = "Gift",
            DiscountAmount = 0
        };

        return MallOrderItem.Create(
            order.Id,
            sku.Id,
            product.Id,
            product.Name,
            sku.SkuCode,
            sku.SpecName,
            0,
            gift.Quantity,
            sku.WeightGram,
            isGift: true,
            promotionId: gift.PromotionId,
            promotionName: gift.PromotionName,
            originalUnitPrice: 0,
            productImage: product.MainImageUrl,
            promotionDiscountAmount: 0,
            promotionSnapshot: BuildPromotionSnapshot(new[] { record }));
    }

    /// <summary>
    /// 行级促销优惠合计必须与订单级促销优惠一致，防止明细与汇总口径分叉。
    /// </summary>
    public static void EnsureLinePromotionTotal(MallOrder order, IEnumerable<MallOrderItem> items)
    {
        var lineTotal = items
            .Where(item => !item.IsGift)
            .Sum(item => item.PromotionDiscountAmount);

        if (Math.Abs(lineTotal - order.PromotionDiscountAmount) > 0.01m)
        {
            throw new BusinessException(
                500,
                $"订单 {order.OrderNo} 促销优惠与明细不一致：明细合计 {lineTotal:0.00}，订单 {order.PromotionDiscountAmount:0.00}");
        }
    }

    private static string BuildPromotionSnapshot(IReadOnlyList<AppliedPromotionRecord> records)
    {
        if (records.Count == 0)
        {
            return "[]";
        }

        var entries = records.Select(record => new PromotionSnapshotEntry
        {
            Id = record.PromotionId,
            Name = record.PromotionName,
            ActionType = record.ActionType,
            Amount = record.DiscountAmount
        });

        return JsonSerializer.Serialize(entries, SnapshotJsonOptions);
    }

    private sealed class PromotionSnapshotEntry
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string ActionType { get; init; } = string.Empty;

        public decimal Amount { get; init; }
    }
}

/// <summary>订单头快照输入。</summary>
public sealed class OrderHeaderSnapshot
{
    public required string OrderNo { get; init; }

    public Guid? MemberId { get; init; }

    public required string ReceiverName { get; init; }

    public required string ReceiverPhone { get; init; }

    public required string ReceiverAddress { get; init; }

    public decimal GoodsAmount { get; init; }

    public decimal PromotionDiscountAmount { get; init; }

    public decimal CouponDiscountAmount { get; init; }

    public Guid? CouponId { get; init; }

    public string CouponName { get; init; } = string.Empty;

    public decimal FreightAmount { get; init; }

    public int TotalWeightGram { get; init; }

    public string Remark { get; init; } = string.Empty;
}
