using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallReturns;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 订单详情装配：订单、门店、明细、支付、退款、物流一次查全。
/// </summary>
internal static class MallOrderDetailLoader
{
    public static async Task<MallOrderDetailDto> LoadAsync(
        IMallDbContext dbContext,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var dto = await dbContext.MallOrders
            .Where(order => order.Id == orderId)
            .Select(order => new MallOrderDetailDto
            {
                    Id = order.Id,
                    OrderNo = order.OrderNo,
                    MemberId = order.MemberId,
                    Status = order.Status,
                    ReceiverName = order.ReceiverName,
                    ReceiverPhone = order.ReceiverPhone,
                    ReceiverAddress = order.ReceiverAddress,
                    Remark = order.Remark,
                    GiftShortageMessage = order.GiftShortageMessage,
                    GoodsAmount = order.GoodsAmount,
                    PromotionDiscountAmount = order.PromotionDiscountAmount,
                    CouponDiscountAmount = order.CouponDiscountAmount,
                    CouponId = order.CouponId,
                    CouponName = order.CouponName,
                    FreightAmount = order.FreightAmount,
                    PayableAmount = order.PayableAmount,
                    PaidAmount = order.PaidAmount,
                    RefundedAmount = order.RefundedAmount,
                    TotalWeightGram = order.TotalWeightGram,
                    CreatedAt = order.CreatedAt,
                    PaidAt = order.PaidAt,
                    ShippedAt = order.ShippedAt,
                    CompletedAt = order.CompletedAt,
                    CancelledAt = order.CancelledAt,
                    RefundedAt = order.RefundedAt
                })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            throw new BusinessException(404, "订单不存在");
        }

        dto.Items = await (
            from item in dbContext.MallOrderItems.AsNoTracking()
            where item.OrderId == orderId
            orderby item.CreatedAt
            select new MallOrderItemDto
            {
                Id = item.Id,
                SkuId = item.SkuId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductImage = item.ProductImage,
                SkuCode = item.SkuCode,
                SpecName = item.SpecName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                WeightGram = item.WeightGram,
                ItemAmount = item.ItemAmount,
                IsGift = item.IsGift,
                PromotionId = item.PromotionId,
                PromotionName = item.PromotionName,
                PromotionSnapshot = item.PromotionSnapshot,
                PromotionDiscountAmount = item.PromotionDiscountAmount,
                OriginalUnitPrice = item.OriginalUnitPrice
            }
        ).ToListAsync(cancellationToken);

        // 金额恒等式校验：商品原价 − 促销优惠 − 券抵扣（封底为 0）+ 运费 = 应付金额
        MallOrder.EnsureAmountIdentity(
            dto.OrderNo,
            dto.GoodsAmount,
            dto.PromotionDiscountAmount,
            dto.CouponDiscountAmount,
            dto.FreightAmount,
            dto.PayableAmount);

        var itemIds = dto.Items.Select(item => item.Id).ToList();
        if (itemIds.Count > 0)
        {
            var reviewedItemIds = await dbContext.MallProductReviews
                .AsNoTracking()
                .Where(review => review.OrderItemId.HasValue && itemIds.Contains(review.OrderItemId.Value))
                .Select(review => review.OrderItemId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
            var reviewedItemIdSet = reviewedItemIds.ToHashSet();

            foreach (var orderItem in dto.Items)
            {
                orderItem.Reviewed = reviewedItemIdSet.Contains(orderItem.Id);
            }
        }

        dto.Payments = await dbContext.MallPayments
            .AsNoTracking()
            .Where(payment => payment.OrderId == orderId)
            .OrderBy(payment => payment.CreatedAt)
            .Select(payment => new MallPaymentDto
            {
                Id = payment.Id,
                PaymentNo = payment.PaymentNo,
                Amount = payment.Amount,
                Method = payment.Method,
                Status = payment.Status,
                TransactionNo = payment.TransactionNo,
                FailureReason = payment.FailureReason,
                PaidAt = payment.PaidAt,
                CreatedAt = payment.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var refundEntities = await dbContext.MallRefunds
            .AsNoTracking()
            .Where(refund => refund.OrderId == orderId)
            .OrderBy(refund => refund.CreatedAt)
            .ToListAsync(cancellationToken);

        dto.Refunds = refundEntities
            .Select(refund => new MallRefundDto
            {
                Id = refund.Id,
                RefundNo = refund.RefundNo,
                PaymentId = refund.PaymentId,
                Amount = refund.Amount,
                Reason = refund.Reason,
                Description = refund.Description,
                EvidenceImages = MallAfterSaleRules.SplitEvidence(refund.EvidenceImages),
                Status = refund.Status,
                RefundedAt = refund.RefundedAt,
                CreatedAt = refund.CreatedAt
            })
            .ToList();

        var returnEntities = await dbContext.MallReturns
            .AsNoTracking()
            .Where(returnEntity => returnEntity.OrderId == orderId)
            .OrderBy(returnEntity => returnEntity.CreatedAt)
            .ToListAsync(cancellationToken);

        dto.Returns = returnEntities.Select(MallReturnMapper.ToDto).ToList();

        dto.Shipment = await dbContext.MallShipments
            .AsNoTracking()
            .Where(shipment => shipment.OrderId == orderId && !shipment.IsVoided)
            .Select(shipment => new MallShipmentDto
            {
                Id = shipment.Id,
                ShipmentNo = shipment.ShipmentNo,
                ExpressCompany = shipment.ExpressCompany,
                TrackingNo = shipment.TrackingNo,
                Status = shipment.Status,
                Remark = shipment.Remark,
                ShippedAt = shipment.ShippedAt,
                DeliveredAt = shipment.DeliveredAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto;
    }
}