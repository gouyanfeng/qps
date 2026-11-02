using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Application.Features.MallStocks;

/// <summary>
/// 分页查询规格库存。
/// </summary>
public class GetMallSkusQuery : PaginationRequest, IRequest<PaginationResponse<MallSkuStockDto>>
{
    /// <summary>
    /// 关键字，匹配规格编码、规格名称、商品编码或商品名称。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 仅看低库存（预警库存大于 0 且当前库存不高于预警库存）。
    /// </summary>
    public bool? LowStock { get; set; }

    /// <summary>
    /// 按商品过滤。
    /// </summary>
    public Guid? ProductId { get; set; }
}

public class GetMallSkusHandler : IRequestHandler<GetMallSkusQuery, PaginationResponse<MallSkuStockDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallSkusHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallSkuStockDto>> Handle(
        GetMallSkusQuery request,
        CancellationToken cancellationToken)
    {
        var query = from sku in _dbContext.MallSkus
                    join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                    where !sku.IsDeleted && !product.IsDeleted
                    select new { Sku = sku, Product = product };

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(row =>
                row.Sku.SkuCode.Contains(keyword) ||
                row.Sku.SpecName.Contains(keyword) ||
                row.Product.Code.Contains(keyword) ||
                row.Product.Name.Contains(keyword));
        }

        if (request.ProductId.HasValue)
        {
            query = query.Where(row => row.Sku.ProductId == request.ProductId.Value);
        }

        if (request.LowStock == true)
        {
            query = query.Where(row => row.Sku.WarnStock > 0 && row.Sku.Stock <= row.Sku.WarnStock);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(row => row.Sku.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var list = rows
            .Select(row => new MallSkuStockDto
            {
                Id = row.Sku.Id,
                ProductId = row.Sku.ProductId,
                ProductCode = row.Product.Code,
                ProductName = row.Product.Name,
                SkuCode = row.Sku.SkuCode,
                SpecName = row.Sku.SpecName,
                Price = row.Sku.Price,
                Stock = row.Sku.Stock,
                WarnStock = row.Sku.WarnStock,
                IsLowStock = row.Sku.WarnStock > 0 && row.Sku.Stock <= row.Sku.WarnStock,
                IsActive = row.Sku.IsActive,
                UpdatedAt = row.Sku.UpdatedAt
            })
            .ToList();

        return new PaginationResponse<MallSkuStockDto>(list, totalCount, page, pageSize);
    }
}
