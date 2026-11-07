using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>商品 DTO 统一映射，供商品列表、详情与活动商品共用。</summary>
internal static class MallProductMapper
{
    public static MallProductDto Map(MallProduct product, string storeName, string categoryName)
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
