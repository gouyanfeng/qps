using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCategories;

/// <summary>
/// 编辑商品分类。
/// </summary>
public class UpdateMallCategoryCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallCategoryUpdateRequest Request { get; set; } = null!;
}

public class UpdateMallCategoryHandler : IRequestHandler<UpdateMallCategoryCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public UpdateMallCategoryHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateMallCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _dbContext.MallCategories
            .FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, cancellationToken);

        if (category is null)
        {
            throw new BusinessException(404, "分类不存在");
        }

        var code = MallCategoryRules.NormalizeCode(request.Request.Code);
        var name = MallCategoryRules.NormalizeName(request.Request.Name);
        MallCategoryRules.ValidateSortOrder(request.Request.SortOrder);

        await MallCategoryRules.EnsureCodeNotDuplicatedAsync(_dbContext, code, request.Id, cancellationToken);
        await MallCategoryRules.EnsureParentValidAsync(
            _dbContext,
            request.Request.ParentId,
            request.Id,
            cancellationToken);

        category.Update(code, name, request.Request.ParentId, request.Request.Icon, request.Request.SortOrder);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}