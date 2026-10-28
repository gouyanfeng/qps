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

    /// <summary>
    /// 按热销状态过滤。
    /// </summary>
    public bool? IsHot { get; set; }

    public string? TagCode { get; set; }
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
                product.Name.Contains(keyword) ||
                product.Tags.Any(tag =>
                    tag.Name.Contains(keyword) &&
                    tag.IsActive &&
                    !tag.IsDeleted));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(product => product.IsActive == request.IsActive.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == request.CategoryId.Value);
        }

        if (request.IsHot.HasValue)
        {
            query = query.Where(product => product.IsHot == request.IsHot.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TagCode))
        {
            query = query.Where(product => product.Tags.Any(tag =>
                tag.Code == request.TagCode && tag.IsActive && !tag.IsDeleted));
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

        var productIds = list.Select(product => product.Id).ToArray();
        if (productIds.Length > 0)
        {
            var skusByProduct = await _dbContext.MallSkus
                .AsNoTracking()
                .Where(sku => productIds.Contains(sku.ProductId) && sku.IsActive && !sku.IsDeleted)
                .OrderBy(sku => sku.CreatedAt)
                .Select(sku => new MallSkuDto
                {
                    Id = sku.Id,
                    ProductId = sku.ProductId,
                    SkuCode = sku.SkuCode,
                    SpecName = sku.SpecName,
                    Price = sku.Price,
                    MarketPrice = sku.MarketPrice,
                    WeightGram = sku.WeightGram,
                    VolumeCm3 = sku.VolumeCm3,
                    Stock = sku.Stock,
                    WarnStock = sku.WarnStock,
                    IsActive = sku.IsActive,
                    CreatedAt = sku.CreatedAt,
                    UpdatedAt = sku.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            var skuLookup = skusByProduct.ToLookup(sku => sku.ProductId);
            foreach (var product in list)
            {
                product.Skus = skuLookup[product.Id].ToList();
            }

            var tagsByProduct = await _dbContext.MallProducts
                .Where(product => productIds.Contains(product.Id))
                .SelectMany(product => product.Tags
                    .Where(tag => tag.IsActive && !tag.IsDeleted)
                    .Select(tag => new
                    {
                        ProductId = product.Id,
                        Tag = new MallTagDto
                        {
                            Id = tag.Id,
                            Code = tag.Code,
                            Name = tag.Name,
                            Color = tag.Color,
                            SortOrder = tag.SortOrder,
                            IsActive = tag.IsActive
                        }
                    }))
                .ToListAsync(cancellationToken);
            var tagLookup = tagsByProduct.ToLookup(item => item.ProductId, item => item.Tag);
            foreach (var product in list)
            {
                product.Tags = tagLookup[product.Id].ToList();
            }
        }

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
            IsHot = product.IsHot,
            Sales = product.Sales,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}