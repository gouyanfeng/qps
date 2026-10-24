using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 删除商场商品（软删除，同时软删除其规格）。
/// </summary>
public class DeleteMallProductCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteMallProductHandler : IRequestHandler<DeleteMallProductCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public DeleteMallProductHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMallProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _dbContext.MallProducts
            .FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, cancellationToken);

        if (product is null)
        {
            throw new BusinessException(404, "商品不存在");
        }

        product.IsDeleted = true;

        var skus = await _dbContext.MallSkus
            .Where(sku => sku.ProductId == request.Id && !sku.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var sku in skus)
        {
            sku.IsDeleted = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
