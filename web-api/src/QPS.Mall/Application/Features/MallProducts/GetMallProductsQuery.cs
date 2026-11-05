using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

public class GetMallProductsQuery : PaginationRequest, IRequest<PaginationResponse<MallProductDto>>
{
    public string? Keyword { get; set; }

    public Guid? StoreId { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CategoryId { get; set; }

    public bool? IsHot { get; set; }

    public string? TagCode { get; set; }

    /// <summary>排序：default 综合序 / sales 销量降序 / price 价格升序。</summary>
    public string? SortBy { get; set; }
}

public class GetMallProductsHandler : IRequestHandler<GetMallProductsQuery, PaginationResponse<MallProductDto>>
{
    private readonly IMallDbContext _dbContext;
    private readonly MallProductPromotionEnricher _enricher;

    public GetMallProductsHandler(IMallDbContext dbContext, MallProductPromotionEnricher enricher)
    {
        _dbContext = dbContext;
        _enricher = enricher;
    }

    public async Task<PaginationResponse<MallProductDto>> Handle(
        GetMallProductsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var query = _dbContext.MallProducts
            .Where(product => !product.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(product =>
                product.Code.Contains(keyword) ||
                product.Name.Contains(keyword) ||
                _dbContext.MallProductTags.Any(pt =>
                    pt.ProductId == product.Id &&
                    !pt.IsDeleted &&
                    _dbContext.MallTags.Any(tag =>
                        tag.Id == pt.TagId &&
                        tag.Name.Contains(keyword) &&
                        tag.IsActive &&
                        !tag.IsDeleted)));
        }

        if (request.StoreId.HasValue && request.StoreId.Value != Guid.Empty)
        {
            query = query.Where(product => product.StoreId == request.StoreId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(product => product.IsActive == request.IsActive.Value);
        }

        if (request.CategoryId.HasValue)
        {
            var targetId = request.CategoryId.Value;
            var expandedIds = await _dbContext.MallCategories
                .Where(c => c.Id == targetId || c.ParentId == targetId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(product =>
                product.CategoryId.HasValue && expandedIds.Contains(product.CategoryId.Value));
        }

        if (request.IsHot.HasValue)
        {
            query = query.Where(product => product.IsHot == request.IsHot.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TagCode))
        {
            var tagCode = request.TagCode;
            query = query.Where(product =>
                _dbContext.MallProductTags.Any(pt =>
                    pt.ProductId == product.Id &&
                    !pt.IsDeleted &&
                    _dbContext.MallTags.Any(tag =>
                        tag.Id == pt.TagId &&
                        tag.Code == tagCode &&
                        tag.IsActive &&
                        !tag.IsDeleted)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortBy switch
        {
            "sales" => query
                .OrderByDescending(product => product.Sales)
                .ThenBy(product => product.SortOrder),
            "price" => query
                .OrderBy(product => _dbContext.MallSkus
                    .Where(sku => sku.ProductId == product.Id && !sku.IsDeleted && sku.IsActive)
                    .Min(sku => (decimal?)sku.Price))
                .ThenBy(product => product.SortOrder),
            _ => query
                .OrderBy(product => product.SortOrder)
                .ThenByDescending(product => product.CreatedAt)
        };

        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new
            {
                Product = product,
                StoreName = _dbContext.MallStores
                    .Where(store => store.Id == product.StoreId)
                    .Select(store => store.Name)
                    .FirstOrDefault(),
                CategoryName = _dbContext.MallCategories
                    .Where(category => category.Id == product.CategoryId)
                    .Select(category => category.Name)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var list = rows
            .Select(row => Map(row.Product, row.StoreName ?? string.Empty, row.CategoryName ?? string.Empty))
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

            var tagsByProduct = await (from pt in _dbContext.MallProductTags
                                       join tag in _dbContext.MallTags on pt.TagId equals tag.Id
                                       where productIds.Contains(pt.ProductId) && !pt.IsDeleted && tag.IsActive && !tag.IsDeleted
                                       select new
                                       {
                                           ProductId = pt.ProductId,
                                           Tag = new MallTagDto
                                           {
                                               Id = tag.Id,
                                               Code = tag.Code,
                                               Name = tag.Name,
                                               Color = tag.Color,
                                               SortOrder = tag.SortOrder,
                                               IsActive = tag.IsActive
                                           }
                                       })
                .ToListAsync(cancellationToken);
            var tagLookup = tagsByProduct.ToLookup(item => item.ProductId, item => item.Tag);
            foreach (var product in list)
            {
                product.Tags = tagLookup[product.Id].ToList();
            }
        }

        await _enricher.EnrichAsync(list, cancellationToken);

        return new PaginationResponse<MallProductDto>(list, totalCount, page, pageSize);
    }

    internal static MallProductDto Map(MallProduct product, string storeName, string categoryName)
    {
        return new MallProductDto
        {
            Id = product.Id,
            StoreId = product.StoreId,
            StoreName = storeName,
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
            FreightTemplateId = product.FreightTemplateId,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}