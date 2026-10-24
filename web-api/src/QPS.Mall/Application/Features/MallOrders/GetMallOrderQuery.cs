using MediatR;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 管理端查询订单详情。
/// </summary>
public class GetMallOrderQuery : IRequest<MallOrderDetailDto>
{
    public Guid Id { get; set; }
}

public class GetMallOrderHandler : IRequestHandler<GetMallOrderQuery, MallOrderDetailDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallOrderHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<MallOrderDetailDto> Handle(GetMallOrderQuery request, CancellationToken cancellationToken)
    {
        return MallOrderDetailLoader.LoadAsync(_dbContext, request.Id, cancellationToken);
    }
}
