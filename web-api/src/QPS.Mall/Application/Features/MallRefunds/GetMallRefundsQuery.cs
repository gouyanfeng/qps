using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;
using QPS.Shared.Extensions;

namespace QPS.Mall.Application.Features.MallRefunds;

/// <summary>
/// 分页查询退款单。
/// </summary>
public class GetMallRefundsQuery : PaginationRequest, IRequest<PaginationResponse<MallRefundListDto>>
{
    public string? Status { get; set; }

    public string? Keyword { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }
}

public class GetMallRefundsHandler : IRequestHandler<GetMallRefundsQuery, PaginationResponse<MallRefundListDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallRefundsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallRefundListDto>> Handle(
        GetMallRefundsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallRefunds
            .Where(r => !r.IsDeleted)
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
                EF.Functions.Like(r.RefundNo, keyword));
        }

        if (request.StartTime.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= request.StartTime.Value);
        }

        if (request.EndTime.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= request.EndTime.Value);
        }

        var dtoQuery = from r in query
                       join o in _dbContext.MallOrders.AsNoTracking() on r.OrderId equals o.Id into orders
                       from o in orders.DefaultIfEmpty()
                       join p in _dbContext.MallPayments.AsNoTracking() on r.PaymentId equals p.Id into payments
                       from p in payments.DefaultIfEmpty()
                       orderby r.CreatedAt descending
                       select new MallRefundListDto
                       {
                           Id = r.Id,
                           RefundNo = r.RefundNo,
                           OrderId = r.OrderId,
                           OrderNo = o != null ? o.OrderNo : string.Empty,
                           PaymentId = r.PaymentId,
                           PaymentNo = p != null ? p.PaymentNo : string.Empty,
                           Amount = r.Amount,
                           Reason = r.Reason,
                           Status = r.Status,
                           RefundedAt = r.RefundedAt,
                           CreatedAt = r.CreatedAt
                       };

        return await dtoQuery.ToPaginationResponseAsync(request, cancellationToken);
    }
}