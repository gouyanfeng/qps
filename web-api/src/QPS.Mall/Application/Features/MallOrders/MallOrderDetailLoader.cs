using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
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
        var dto = await (
                from order in dbContext.MallOrders
                join store in dbContext.MallStores on order.StoreId equals store.Id
                where order.Id == orderId && !order.IsDeleted
                select new MallOrderDetailDto
                {
                    Id = order.Id,
                    OrderNo = order.OrderNo,
                    StoreId = order.StoreId,
                    StoreName = store.Name,
                    Status = order.Status,
                    ReceiverName = order.ReceiverName,
                    ReceiverPhone = order.ReceiverPhone,
                    ReceiverAddress = order.ReceiverAddress,
                    Remark = order.Remark,
                    GoodsAmount = order.GoodsAmount,
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

        dto.Items = await dbContext.MallOrderItems
            .AsNoTracking()
            .Where(item => item.OrderId == orderId && !item.IsDeleted)
            .OrderBy(item => item.CreatedAt)
            .Select(item => new MallOrderItemDto
            {
                Id = item.Id,
                SkuId = item.SkuId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                SkuCode = item.SkuCode,
                SpecName = item.SpecName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                WeightGram = item.WeightGram,
                ItemAmount = item.ItemAmount
            })
            .ToListAsync(cancellationToken);

        dto.Payments = await dbContext.MallPayments
            .AsNoTracking()
            .Where(payment => payment.OrderId == orderId && !payment.IsDeleted)
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

        dto.Refunds = await dbContext.MallRefunds
            .AsNoTracking()
            .Where(refund => refund.OrderId == orderId && !refund.IsDeleted)
            .OrderBy(refund => refund.CreatedAt)
            .Select(refund => new MallRefundDto
            {
                Id = refund.Id,
                RefundNo = refund.RefundNo,
                PaymentId = refund.PaymentId,
                Amount = refund.Amount,
                Reason = refund.Reason,
                Status = refund.Status,
                RefundedAt = refund.RefundedAt,
                CreatedAt = refund.CreatedAt
            })
            .ToListAsync(cancellationToken);

        dto.Shipment = await dbContext.MallShipments
            .AsNoTracking()
            .Where(shipment => shipment.OrderId == orderId && !shipment.IsDeleted)
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
