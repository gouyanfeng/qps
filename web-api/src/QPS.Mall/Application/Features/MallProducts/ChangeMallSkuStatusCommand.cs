using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 规格启用或停用。
/// </summary>
public class ChangeMallSkuStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallSkuStatusRequest Request { get; set; } = null!;
}

public class ChangeMallSkuStatusHandler : IRequestHandler<ChangeMallSkuStatusCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ChangeMallSkuStatusHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(ChangeMallSkuStatusCommand request, CancellationToken cancellationToken)
    {
        var sku = await _dbContext.MallSkus
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (sku is null)
        {
            throw new BusinessException(404, "规格不存在");
        }

        sku.ChangeStatus(request.Request.IsActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
