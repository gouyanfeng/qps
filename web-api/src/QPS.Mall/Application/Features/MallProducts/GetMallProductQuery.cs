using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 查询商场商品详情（含规格）。
/// </summary>
public class GetMallProductQuery : IRequest<MallProductDto>
{
    public Guid Id { get; set; }
}

public class GetMallProductHandler : IRequestHandler<GetMallProductQuery, MallProductDto>
{
    private readonly IMallDbContext _dbContext;
    private readonly MallProductPromotionEnricher _enricher;

    public GetMallProductHandler(IMallDbContext dbContext, MallProductPromotionEnricher enricher)
    {
        _dbContext = dbContext;
        _enricher = enricher;
    }

    public async Task<MallProductDto> Handle(GetMallProductQuery request, CancellationToken cancellationToken)
    {
        var row = await _dbContext.MallProducts
            .AsNoTracking()
            .Where(product => product.Id == request.Id && !product.IsDeleted)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            throw new BusinessException(404, "商品不存在");
        }

        var dto = GetMallProductsHandler.Map(row.Product, row.StoreName ?? string.Empty, row.CategoryName ?? string.Empty);

        dto.Skus = await _dbContext.MallSkus
            .AsNoTracking()
            .Where(sku => sku.ProductId == request.Id && !sku.IsDeleted)
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
                Stock = sku.Stock,
                WarnStock = sku.WarnStock,
                IsActive = sku.IsActive,
                CreatedAt = sku.CreatedAt,
                UpdatedAt = sku.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        dto.Tags = await (from link in _dbContext.MallProductTags
                          join tag in _dbContext.MallTags on link.TagId equals tag.Id
                          where link.ProductId == request.Id && !link.IsDeleted && tag.IsActive && !tag.IsDeleted
                          orderby tag.SortOrder
                          select new MallTagDto
                          {
                              Id = tag.Id,
                              Code = tag.Code,
                              Name = tag.Name,
                              Color = tag.Color,
                              SortOrder = tag.SortOrder,
                              IsActive = tag.IsActive
                          })
            .ToListAsync(cancellationToken);

        await _enricher.EnrichAsync(new[] { dto }, cancellationToken);

        return dto;
    }
}