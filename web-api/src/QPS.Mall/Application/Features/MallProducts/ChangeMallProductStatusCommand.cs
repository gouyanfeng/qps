using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 商品上下架。
/// </summary>
public class ChangeMallProductStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallProductStatusRequest Request { get; set; } = null!;
}

public class ChangeMallProductStatusHandler : IRequestHandler<ChangeMallProductStatusCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ChangeMallProductStatusHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(ChangeMallProductStatusCommand request, CancellationToken cancellationToken)
    {
        var product = await _dbContext.MallProducts
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new BusinessException(404, "商品不存在");
        }

        product.ChangeStatus(request.Request.IsActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
