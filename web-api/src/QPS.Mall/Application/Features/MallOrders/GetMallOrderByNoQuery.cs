using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 外部系统按订单号查询订单详情。
/// </summary>
public class GetMallOrderByNoQuery : IRequest<MallOrderDetailDto>
{
    public string OrderNo { get; set; } = string.Empty;
}

public class GetMallOrderByNoHandler : IRequestHandler<GetMallOrderByNoQuery, MallOrderDetailDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallOrderByNoHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallOrderDetailDto> Handle(
        GetMallOrderByNoQuery request,
        CancellationToken cancellationToken)
    {
        var orderNo = request.OrderNo?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(orderNo))
        {
            throw new BusinessException(400, "订单号不能为空");
        }

        var orderId = await _dbContext.MallOrders
            .AsNoTracking()
            .Where(order => order.OrderNo == orderNo && !order.IsDeleted)
            .Select(order => (Guid?)order.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!orderId.HasValue)
        {
            throw new BusinessException(404, "订单不存在");
        }

        return await MallOrderDetailLoader.LoadAsync(_dbContext, orderId.Value, cancellationToken);
    }
}
