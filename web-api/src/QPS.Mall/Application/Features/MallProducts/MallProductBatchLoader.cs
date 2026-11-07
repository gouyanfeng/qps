using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 商品列表批量装载：统一补齐启用规格与启用标签，保证各列表入口的商品数据一致。
/// </summary>
internal static class MallProductBatchLoader
{
    /// <summary>
    /// 为商品 DTO 集合批量装载规格（仅启用）与标签（仅启用 Tag）。
    /// </summary>
    public static async Task LoadSkusAndTagsAsync(
        IMallDbContext dbContext,
        IReadOnlyCollection<MallProductDto> products,
        CancellationToken cancellationToken)
    {
        if (products.Count == 0)
        {
            return;
        }

        var productIds = products.Select(product => product.Id).ToArray();

        var skus = await dbContext.MallSkus
            .AsNoTracking()
            .Where(sku => productIds.Contains(sku.ProductId) && sku.IsActive)
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

        var skuLookup = skus.ToLookup(sku => sku.ProductId);
        foreach (var product in products)
        {
            product.Skus = skuLookup[product.Id].ToList();
        }

        var tags = await (from pt in dbContext.MallProductTags
                          join tag in dbContext.MallTags on pt.TagId equals tag.Id
                          where productIds.Contains(pt.ProductId) && tag.IsActive
                          select new
                          {
                              ProductId = pt.ProductId,
                              Tag = new MallTagDto
                              {
                                  Id = tag.Id,
                                  Type = tag.Type,
                                  Code = tag.Code,
                                  Name = tag.Name,
                                  Color = tag.Color,
                                  SortOrder = tag.SortOrder,
                                  IsActive = tag.IsActive
                              }
                          })
            .ToListAsync(cancellationToken);

        var tagLookup = tags.ToLookup(item => item.ProductId, item => item.Tag);
        foreach (var product in products)
        {
            product.Tags = tagLookup[product.Id].ToList();
        }
    }
}
