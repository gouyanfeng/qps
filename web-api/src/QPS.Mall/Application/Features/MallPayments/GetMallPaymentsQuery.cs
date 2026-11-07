using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;
using QPS.Shared.Extensions;

namespace QPS.Mall.Application.Features.MallPayments;

/// <summary>
/// 分页查询支付单。
/// </summary>
public class GetMallPaymentsQuery : PaginationRequest, IRequest<PaginationResponse<MallPaymentListDto>>
{
    public string? Status { get; set; }

    public string? Keyword { get; set; }

    public string? Method { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }
}

public class GetMallPaymentsHandler : IRequestHandler<GetMallPaymentsQuery, PaginationResponse<MallPaymentListDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallPaymentsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallPaymentListDto>> Handle(
        GetMallPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallPayments
            
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Method))
        {
            var method = request.Method.Trim();
            query = query.Where(p => p.Method == method);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = $"%{request.Keyword.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.PaymentNo, keyword));
        }

        if (request.StartTime.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= request.StartTime.Value);
        }

        if (request.EndTime.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= request.EndTime.Value);
        }

        var dtoQuery = from p in query
                       join o in _dbContext.MallOrders.AsNoTracking() on p.OrderId equals o.Id into orders
                       from o in orders.DefaultIfEmpty()
                       orderby p.CreatedAt descending
                       select new MallPaymentListDto
                       {
                           Id = p.Id,
                           PaymentNo = p.PaymentNo,
                           OrderId = p.OrderId,
                           OrderNo = o != null ? o.OrderNo : string.Empty,
                           Amount = p.Amount,
                           Method = p.Method,
                           Status = p.Status,
                           TransactionNo = p.TransactionNo,
                           FailureReason = p.FailureReason,
                           PaidAt = p.PaidAt,
                           CreatedAt = p.CreatedAt
                       };

        return await dtoQuery.ToPaginationResponseAsync(request, cancellationToken);
    }
}