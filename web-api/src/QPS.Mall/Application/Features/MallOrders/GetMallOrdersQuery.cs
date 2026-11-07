using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Common;
using QPS.Shared.Extensions;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 管理端分页查询订单。
/// </summary>
public class GetMallOrdersQuery : PaginationRequest, IRequest<PaginationResponse<MallOrderDto>>
{
    public GetMallOrdersQuery()
    {
        SortField = nameof(MallOrderDto.CreatedAt);
        SortDirection = "Descending";
    }

    /// <summary>
    /// 订单状态。
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 会员ID，C端过滤自己的订单。
    /// </summary>
    public Guid? MemberId { get; set; }

    /// <summary>
    /// 关键字，匹配订单号、收货人或电话。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 下单开始时间。
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 下单结束时间。
    /// </summary>
    public DateTime? EndTime { get; set; }
}

public class GetMallOrdersHandler : IRequestHandler<GetMallOrdersQuery, PaginationResponse<MallOrderDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallOrdersHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallOrderDto>> Handle(
        GetMallOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallOrders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            // 支持逗号分隔的状态组，如「退款/售后」tab 一次筛出多个售后状态
            var statuses = request.Status
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = query.Where(order => statuses.Contains(order.Status));
        }

        if (request.MemberId.HasValue)
        {
            query = query.Where(order => order.MemberId == request.MemberId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(order =>
                order.OrderNo.Contains(keyword) ||
                order.ReceiverName.Contains(keyword) ||
                order.ReceiverPhone.Contains(keyword));
        }

        if (request.StartTime.HasValue)
        {
            query = query.Where(order => order.CreatedAt >= request.StartTime.Value);
        }

        if (request.EndTime.HasValue)
        {
            query = query.Where(order => order.CreatedAt <= request.EndTime.Value);
        }

        var dtoQuery = query.Select(order => new MallOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            MemberId = order.MemberId,
            Status = order.Status,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = order.ReceiverPhone,
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
            CancelledAt = order.CancelledAt
        });

        var page = await dtoQuery.ToPaginationResponseAsync(request, cancellationToken);

        // 列表同样校验金额恒等式：存量数据不自洽时直接暴露，不静默展示错误金额
        foreach (var order in page.List)
        {
            MallOrder.EnsureAmountIdentity(
                order.OrderNo,
                order.GoodsAmount,
                order.PromotionDiscountAmount,
                order.CouponDiscountAmount,
                order.FreightAmount,
                order.PayableAmount);
        }

        var memberIds = page.List
            .Where(order => order.MemberId.HasValue)
            .Select(order => order.MemberId!.Value)
            .Distinct()
            .ToList();
        if (memberIds.Count > 0)
        {
            var members = await _dbContext.MallMembers
                .AsNoTracking()
                .Where(member => memberIds.Contains(member.Id))
                .Select(member => new { member.Id, member.Nickname, member.Phone })
                .ToListAsync(cancellationToken);
            var memberMap = members.ToDictionary(
                member => member.Id,
                member => string.IsNullOrWhiteSpace(member.Nickname) ? member.Phone : member.Nickname);

            foreach (var order in page.List)
            {
                if (order.MemberId.HasValue && memberMap.TryGetValue(order.MemberId.Value, out var memberName))
                {
                    order.MemberName = memberName;
                }
            }
        }

        var orderIds = page.List.Select(o => o.Id).ToList();
        if (orderIds.Count > 0)
        {
            var items = await (
                from i in _dbContext.MallOrderItems.AsNoTracking()
                where orderIds.Contains(i.OrderId)
                orderby i.CreatedAt
                select new { i.OrderId, Dto = new MallOrderItemDto
                {
                    Id = i.Id,
                    SkuId = i.SkuId,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductImage = i.ProductImage,
                    SpecName = i.SpecName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    ItemAmount = i.ItemAmount,
                    IsGift = i.IsGift,
                    PromotionId = i.PromotionId,
                    PromotionName = i.PromotionName,
                    PromotionSnapshot = i.PromotionSnapshot,
                    PromotionDiscountAmount = i.PromotionDiscountAmount,
                    OriginalUnitPrice = i.OriginalUnitPrice
                }}
            ).ToListAsync(cancellationToken);

            var itemMap = items.GroupBy(x => x.OrderId).ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());

            // 列表也带真实运单信息，移动端不再用硬编码物流文案
            var shipmentMap = (await _dbContext.MallShipments
                    .AsNoTracking()
                    .Where(shipment => orderIds.Contains(shipment.OrderId) && !shipment.IsVoided)
                    .Select(shipment => new { shipment.OrderId, Dto = new MallShipmentDto
                    {
                        Id = shipment.Id,
                        ShipmentNo = shipment.ShipmentNo,
                        ExpressCompany = shipment.ExpressCompany,
                        TrackingNo = shipment.TrackingNo,
                        Status = shipment.Status,
                        Remark = shipment.Remark,
                        ShippedAt = shipment.ShippedAt,
                        DeliveredAt = shipment.DeliveredAt
                    }})
                    .ToListAsync(cancellationToken))
                .ToDictionary(x => x.OrderId, x => x.Dto);

            var itemIds = items.Select(x => x.Dto.Id).ToList();
            var reviewedItemIds = await _dbContext.MallProductReviews
                .AsNoTracking()
                .Where(review => review.OrderItemId.HasValue && itemIds.Contains(review.OrderItemId.Value))
                .Select(review => review.OrderItemId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
            var reviewedItemIdSet = reviewedItemIds.ToHashSet();

            foreach (var order in page.List)
            {
                if (shipmentMap.TryGetValue(order.Id, out var shipment))
                {
                    order.Shipment = shipment;
                }

                if (itemMap.TryGetValue(order.Id, out var orderItems))
                {
                    foreach (var orderItem in orderItems)
                    {
                        orderItem.Reviewed = reviewedItemIdSet.Contains(orderItem.Id);
                    }

                    order.Items = orderItems;
                }
            }
        }

        return page;
    }
}