using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 删除商品规格（软删除）。
/// </summary>
public class DeleteMallSkuCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteMallSkuHandler : IRequestHandler<DeleteMallSkuCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public DeleteMallSkuHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMallSkuCommand request, CancellationToken cancellationToken)
    {
        var sku = await _dbContext.MallSkus
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (sku is null)
        {
            throw new BusinessException(404, "规格不存在");
        }

        sku.SoftDelete();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
