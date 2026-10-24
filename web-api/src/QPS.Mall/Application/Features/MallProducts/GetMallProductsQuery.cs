using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Common;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 分页查询商场商品。
/// </summary>
public class GetMallProductsQuery : PaginationRequest, IRequest<PaginationResponse<MallProductDto>>
{
    /// <summary>
    /// 关键字，匹配商品编码或名称。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 按上下架状态过滤。
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// 按分类过滤。
    /// </summary>
    public Guid? CategoryId { get; set; }
}

public class GetMallProductsHandler : IRequestHandler<GetMallProductsQuery, PaginationResponse<MallProductDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallProductsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallProductDto>> Handle(
        GetMallProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallProducts
            .Where(product => !product.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(product =>
                product.Code.Contains(keyword) ||
                product.Name.Contains(keyword));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(product => product.IsActive == request.IsActive.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == request.CategoryId.Value);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderBy(product => product.SortOrder)
            .ThenByDescending(product => product.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new
            {
                Product = product,
                CategoryName = _dbContext.MallCategories
                    .Where(category => category.Id == product.CategoryId)
                    .Select(category => category.Name)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var list = rows
            .Select(row => Map(row.Product, row.CategoryName ?? string.Empty))
            .ToList();

        return new PaginationResponse<MallProductDto>(list, totalCount, page, pageSize);
    }

    internal static MallProductDto Map(MallProduct product, string categoryName)
    {
        return new MallProductDto
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            Description = product.Description,
            CategoryId = product.CategoryId,
            CategoryName = categoryName,
            MainImageUrl = product.MainImageUrl,
            AlbumImages = MallProductRules.DeserializeAlbumImages(product.AlbumImages),
            DetailHtml = product.DetailHtml,
            SortOrder = product.SortOrder,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
