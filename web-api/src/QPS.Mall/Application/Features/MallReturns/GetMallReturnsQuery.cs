using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;
using QPS.Shared.Extensions;

namespace QPS.Mall.Application.Features.MallReturns;

public class GetMallReturnsQuery : PaginationRequest, IRequest<PaginationResponse<MallReturnListDto>>
{
    public string? Status { get; set; }

    public string? Keyword { get; set; }

    public Guid? StoreId { get; set; }
}

public class GetMallReturnsHandler : IRequestHandler<GetMallReturnsQuery, PaginationResponse<MallReturnListDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallReturnsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallReturnListDto>> Handle(
        GetMallReturnsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallReturns
            
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = $"%{request.Keyword.Trim()}%";
            query = query.Where(r =>
                EF.Functions.Like(r.ReturnNo, keyword));
        }

        var dtoQuery = from r in query
                       join o in _dbContext.MallOrders.AsNoTracking() on r.OrderId equals o.Id into orders
                       from o in orders.DefaultIfEmpty()
                       orderby r.CreatedAt descending
                       select new MallReturnListDto
                       {
                           Id = r.Id,
                           ReturnNo = r.ReturnNo,
                           OrderId = r.OrderId,
                           OrderNo = o != null ? o.OrderNo : string.Empty,
                           UserName = string.Empty,
                           ProductName = string.Empty,
                           Quantity = 0,
                           RefundAmount = r.Amount,
                           Reason = r.Reason,
                           Status = r.Status,
                           CreatedAt = r.CreatedAt
                       };

        return await dtoQuery.ToPaginationResponseAsync(request, cancellationToken);
    }
}

public class GetMallReturnQuery : IRequest<MallReturnDetailDto>
{
    public Guid Id { get; set; }
}

public class GetMallReturnHandler : IRequestHandler<GetMallReturnQuery, MallReturnDetailDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallReturnHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallReturnDetailDto> Handle(GetMallReturnQuery request, CancellationToken cancellationToken)
    {
        var r = await _dbContext.MallReturns
            .Where(x => x.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (r == null)
            throw new QPS.Shared.Exceptions.BusinessException(404, "退货单不存在");

        var order = await _dbContext.MallOrders.AsNoTracking()
            .Where(o => o.Id == r.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        return new MallReturnDetailDto
        {
            Id = r.Id,
            ReturnNo = r.ReturnNo,
            OrderId = r.OrderId,
            OrderNo = order?.OrderNo ?? string.Empty,
            UserName = string.Empty,
            ProductName = string.Empty,
            Quantity = 0,
            RefundAmount = r.Amount,
            Reason = r.Reason,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            UserPhone = string.Empty,
            SkuSpec = string.Empty,
            Description = null,
            RejectReason = null,
            TrackingCompany = r.ExpressCompany,
            TrackingNo = r.TrackingNo
        };
    }
}