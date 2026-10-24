using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCategories;

/// <summary>
/// 商品分类启用或停用。
/// </summary>
public class ChangeMallCategoryStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallCategoryStatusRequest Request { get; set; } = null!;
}

public class ChangeMallCategoryStatusHandler : IRequestHandler<ChangeMallCategoryStatusCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ChangeMallCategoryStatusHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(
        ChangeMallCategoryStatusCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.MallCategories
            .FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, cancellationToken);

        if (category is null)
        {
            throw new BusinessException(404, "分类不存在");
        }

        category.ChangeStatus(request.Request.IsActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
