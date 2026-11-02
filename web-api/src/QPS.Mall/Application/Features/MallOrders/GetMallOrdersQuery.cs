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
        var query = _dbContext.MallOrders.Where(order => !order.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(order => order.Status == status);
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
            PromotionDiscountAmount = _dbContext.MallOrderItems
                .Where(orderItem => orderItem.OrderId == order.Id && !orderItem.IsDeleted &&
                                    !orderItem.IsGift && orderItem.OriginalUnitPrice.HasValue)
                .Sum(orderItem => (orderItem.OriginalUnitPrice!.Value - orderItem.UnitPrice) * orderItem.Quantity),
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

        var orderIds = page.List.Select(o => o.Id).ToList();
        if (orderIds.Count > 0)
        {
            var items = await (
                from i in _dbContext.MallOrderItems.AsNoTracking()
                where orderIds.Contains(i.OrderId) && !i.IsDeleted
                join p in _dbContext.MallProducts.AsNoTracking() on i.ProductId equals p.Id into products
                from p in products.DefaultIfEmpty()
                orderby i.CreatedAt
                select new { i.OrderId, Dto = new MallOrderItemDto
                {
                    Id = i.Id,
                    SkuId = i.SkuId,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductImage = p != null ? p.MainImageUrl : string.Empty,
                    SpecName = i.SpecName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    ItemAmount = i.ItemAmount,
                    IsGift = i.IsGift,
                    PromotionName = i.PromotionName,
                    OriginalUnitPrice = i.OriginalUnitPrice
                }}
            ).ToListAsync(cancellationToken);

            var itemMap = items.GroupBy(x => x.OrderId).ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());
            foreach (var order in page.List)
            {
                if (itemMap.TryGetValue(order.Id, out var orderItems))
                    order.Items = orderItems;
            }
        }

        return page;
    }
}