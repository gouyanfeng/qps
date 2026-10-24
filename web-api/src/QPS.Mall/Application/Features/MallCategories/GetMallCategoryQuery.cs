using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCategories;

/// <summary>
/// 查询商品分类详情。
/// </summary>
public class GetMallCategoryQuery : IRequest<MallCategoryDto>
{
    public Guid Id { get; set; }
}

public class GetMallCategoryHandler : IRequestHandler<GetMallCategoryQuery, MallCategoryDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallCategoryHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallCategoryDto> Handle(GetMallCategoryQuery request, CancellationToken cancellationToken)
    {
        var dto = await _dbContext.MallCategories
            .AsNoTracking()
            .Where(category => category.Id == request.Id && !category.IsDeleted)
            .Select(category => new MallCategoryDto
            {
                Id = category.Id,
                Code = category.Code,
                Name = category.Name,
                SortOrder = category.SortOrder,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            throw new BusinessException(404, "分类不存在");
        }

        return dto;
    }
}
