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

    public GetMallProductHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallProductDto> Handle(GetMallProductQuery request, CancellationToken cancellationToken)
    {
        var row = await _dbContext.MallProducts
            .AsNoTracking()
            .Where(product => product.Id == request.Id && !product.IsDeleted)
            .Select(product => new
            {
                Product = product,
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

        var dto = GetMallProductsHandler.Map(row.Product, row.CategoryName ?? string.Empty);

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
                WeightGram = sku.WeightGram,
                Stock = sku.Stock,
                WarnStock = sku.WarnStock,
                IsActive = sku.IsActive,
                CreatedAt = sku.CreatedAt,
                UpdatedAt = sku.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return dto;
    }
}
