using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
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
    /// 门店。
    /// </summary>
    public Guid? StoreId { get; set; }

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
        var query =
            from order in _dbContext.MallOrders
            join store in _dbContext.MallStores on order.StoreId equals store.Id
            where !order.IsDeleted
            select new { Order = order, Store = store };

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(item => item.Order.Status == status);
        }

        if (request.StoreId.HasValue)
        {
            query = query.Where(item => item.Order.StoreId == request.StoreId.Value);
        }

        if (request.MemberId.HasValue)
        {
            query = query.Where(item => item.Order.MemberId == request.MemberId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(item =>
                item.Order.OrderNo.Contains(keyword) ||
                item.Order.ReceiverName.Contains(keyword) ||
                item.Order.ReceiverPhone.Contains(keyword));
        }

        if (request.StartTime.HasValue)
        {
            query = query.Where(item => item.Order.CreatedAt >= request.StartTime.Value);
        }

        if (request.EndTime.HasValue)
        {
            query = query.Where(item => item.Order.CreatedAt <= request.EndTime.Value);
        }

        var dtoQuery = query.Select(item => new MallOrderDto
        {
            Id = item.Order.Id,
            OrderNo = item.Order.OrderNo,
            StoreId = item.Order.StoreId,
            MemberId = item.Order.MemberId,
            StoreName = item.Store.Name,
            Status = item.Order.Status,
            ReceiverName = item.Order.ReceiverName,
            ReceiverPhone = item.Order.ReceiverPhone,
            GoodsAmount = item.Order.GoodsAmount,
            PromotionDiscountAmount = _dbContext.MallOrderItems
                .Where(orderItem => orderItem.OrderId == item.Order.Id && !orderItem.IsDeleted &&
                                    !orderItem.IsGift && orderItem.OriginalUnitPrice.HasValue)
                .Sum(orderItem => (orderItem.OriginalUnitPrice!.Value - orderItem.UnitPrice) * orderItem.Quantity),
            FreightAmount = item.Order.FreightAmount,
            PayableAmount = item.Order.PayableAmount,
            PaidAmount = item.Order.PaidAmount,
            RefundedAmount = item.Order.RefundedAmount,
            TotalWeightGram = item.Order.TotalWeightGram,
            CreatedAt = item.Order.CreatedAt,
            PaidAt = item.Order.PaidAt,
            ShippedAt = item.Order.ShippedAt,
            CompletedAt = item.Order.CompletedAt,
            CancelledAt = item.Order.CancelledAt
        });

        return await dtoQuery.ToPaginationResponseAsync(request, cancellationToken);
    }
}