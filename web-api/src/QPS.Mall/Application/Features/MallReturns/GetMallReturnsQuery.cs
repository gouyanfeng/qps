using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallReturns;

public class GetMallReturnsQuery : PaginationRequest, IRequest<PaginationResponse<MallReturnDto>>
{
    public string? Status { get; set; }

    public string? Keyword { get; set; }

    public Guid? StoreId { get; set; }
}

public class GetMallReturnsHandler : IRequestHandler<GetMallReturnsQuery, PaginationResponse<MallReturnDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallReturnsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallReturnDto>> Handle(
        GetMallReturnsQuery request,
        CancellationToken cancellationToken)
    {
        var query = from r in _dbContext.MallReturns.AsNoTracking()
                    join o in _dbContext.MallOrders.AsNoTracking()
                        on r.OrderId equals o.Id into orders
                    from o in orders.DefaultIfEmpty()
                    select new { Return = r, Order = o };

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(item => item.Return.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = $"%{request.Keyword.Trim()}%";
            query = query.Where(item =>
                EF.Functions.Like(item.Return.ReturnNo, keyword) ||
                (item.Order != null &&
                 (EF.Functions.Like(item.Order.OrderNo, keyword) ||
                  EF.Functions.Like(item.Order.ReceiverName, keyword) ||
                  EF.Functions.Like(item.Order.ReceiverPhone, keyword))));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;
        var page = request.Page <= 0 ? 1 : request.Page;

        var rows = await query
            .OrderByDescending(item => item.Return.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var orderIds = rows
            .Where(item => item.Order != null)
            .Select(item => item.Return.OrderId)
            .Distinct()
            .ToList();

        var items = orderIds.Count == 0
            ? new List<MallOrderItem>()
            : await _dbContext.MallOrderItems
                .AsNoTracking()
                .Where(item => orderIds.Contains(item.OrderId))
                .ToListAsync(cancellationToken);

        var itemsByOrder = items
            .GroupBy(item => item.OrderId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var list = rows.Select(item =>
        {
            var dto = MallReturnMapper.ToDto(item.Return);

            dto.OrderNo = item.Order?.OrderNo ?? string.Empty;
            dto.UserName = item.Order?.ReceiverName ?? string.Empty;
            dto.UserPhone = item.Order?.ReceiverPhone ?? string.Empty;

            itemsByOrder.TryGetValue(item.Return.OrderId, out var orderItems);

            if (orderItems is { Count: > 0 })
            {
                dto.ProductName = string.Join("、", orderItems
                    .Select(orderItem => orderItem.ProductName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct());
                dto.SkuSpec = string.Join("、", orderItems
                    .Select(orderItem => orderItem.SpecName)
                    .Where(spec => !string.IsNullOrWhiteSpace(spec))
                    .Distinct());
                dto.Quantity = orderItems.Sum(orderItem => orderItem.Quantity);
            }

            return dto;
        }).ToList();

        return new PaginationResponse<MallReturnDto>(list, totalCount, page, pageSize);
    }
}

public class GetMallReturnQuery : IRequest<MallReturnDto>
{
    public Guid Id { get; set; }
}

public class GetMallReturnHandler : IRequestHandler<GetMallReturnQuery, MallReturnDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallReturnHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallReturnDto> Handle(GetMallReturnQuery request, CancellationToken cancellationToken)
    {
        var r = await _dbContext.MallReturns
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (r == null)
            throw new BusinessException(404, "退货单不存在");

        var order = await _dbContext.MallOrders.AsNoTracking()
            .Where(o => o.Id == r.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        var items = await _dbContext.MallOrderItems.AsNoTracking()
            .Where(item => item.OrderId == r.OrderId)
            .ToListAsync(cancellationToken);

        var dto = MallReturnMapper.ToDto(r);

        dto.OrderNo = order?.OrderNo ?? string.Empty;
        dto.UserName = order?.ReceiverName ?? string.Empty;
        dto.UserPhone = order?.ReceiverPhone ?? string.Empty;
        dto.ProductName = string.Join("、", items
            .Select(item => item.ProductName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct());
        dto.SkuSpec = string.Join("、", items
            .Select(item => item.SpecName)
            .Where(spec => !string.IsNullOrWhiteSpace(spec))
            .Distinct());
        dto.Quantity = items.Sum(item => item.Quantity);

        return dto;
    }
}
