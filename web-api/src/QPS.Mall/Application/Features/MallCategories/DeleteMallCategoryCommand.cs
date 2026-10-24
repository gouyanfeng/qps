using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCategories;

/// <summary>
/// 删除商品分类（软删除）。
/// </summary>
public class DeleteMallCategoryCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteMallCategoryHandler : IRequestHandler<DeleteMallCategoryCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public DeleteMallCategoryHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMallCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _dbContext.MallCategories
            .FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, cancellationToken);

        if (category is null)
        {
            throw new BusinessException(404, "分类不存在");
        }

        var hasProducts = await _dbContext.MallProducts
            .AnyAsync(product => product.CategoryId == request.Id && !product.IsDeleted, cancellationToken);

        if (hasProducts)
        {
            throw new BusinessException(400, "分类下存在商品，无法删除");
        }

        category.IsDeleted = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
