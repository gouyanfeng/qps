using MediatR;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallCategories;

/// <summary>
/// 新增商品分类。
/// </summary>
public class CreateMallCategoryCommand : IRequest<bool>
{
    public MallCategoryCreateRequest Request { get; set; } = null!;
}

public class CreateMallCategoryHandler : IRequestHandler<CreateMallCategoryCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public CreateMallCategoryHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(CreateMallCategoryCommand request, CancellationToken cancellationToken)
    {
        var code = MallCategoryRules.NormalizeCode(request.Request.Code);
        var name = MallCategoryRules.NormalizeName(request.Request.Name);
        MallCategoryRules.ValidateSortOrder(request.Request.SortOrder);

        await MallCategoryRules.EnsureCodeNotDuplicatedAsync(_dbContext, code, null, cancellationToken);

        _dbContext.MallCategories.Add(
            MallCategory.Create(code, name, request.Request.SortOrder, request.Request.IsActive));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
