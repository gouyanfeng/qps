using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Common;
using QPS.Shared.Extensions;

namespace QPS.Mall.Application.Features.MallCategories;

/// <summary>
/// 分页查询商品分类。
/// </summary>
public class GetMallCategoriesQuery : PaginationRequest, IRequest<PaginationResponse<MallCategoryDto>>
{
    public GetMallCategoriesQuery()
    {
        SortField = nameof(MallCategoryDto.SortOrder);
        SortDirection = "Ascending";
    }

    /// <summary>
    /// 关键字，匹配分类编码或名称。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 按门店过滤。
    /// </summary>
    public Guid? StoreId { get; set; }

    /// <summary>
    /// 按启用状态过滤。
    /// </summary>
    public bool? IsActive { get; set; }
}

public class GetMallCategoriesHandler : IRequestHandler<GetMallCategoriesQuery, PaginationResponse<MallCategoryDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallCategoriesHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallCategoryDto>> Handle(
        GetMallCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallCategories
            .Where(category => !category.IsDeleted)
            .AsQueryable();

        if (request.StoreId.HasValue)
        {
            query = query.Where(category => category.StoreId == request.StoreId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(category =>
                category.Code.Contains(keyword) ||
                category.Name.Contains(keyword));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(category => category.IsActive == request.IsActive.Value);
        }

        var dtoQuery = query.Select(category => new MallCategoryDto
        {
            Id = category.Id,
            StoreId = category.StoreId,
            Code = category.Code,
            Name = category.Name,
            ParentId = category.ParentId,
            ParentName = _dbContext.MallCategories
                .Where(parent => parent.Id == category.ParentId)
                .Select(parent => parent.Name)
                .FirstOrDefault() ?? string.Empty,
            Icon = category.Icon,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        });

        return await dtoQuery.ToPaginationResponseAsync(request, cancellationToken);
    }
}
