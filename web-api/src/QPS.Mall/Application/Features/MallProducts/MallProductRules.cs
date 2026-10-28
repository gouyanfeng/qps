using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 商品与规格字段规范化与校验。
/// </summary>
internal static class MallProductRules
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int SkuCodeMaxLength = 50;
    public const int SpecNameMaxLength = 100;
    public const int ImageUrlMaxLength = 500;
    public const int MaxAlbumImages = 9;
    public const int DetailHtmlMaxLength = 50000;
    public const int MaxSortOrder = 9999;

    public static string NormalizeCode(string? code)
    {
        var normalized = code?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, "请输入商品编码");
        }

        if (normalized.Length > CodeMaxLength)
        {
            throw new BusinessException(400, $"商品编码不能超过 {CodeMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeName(string? name)
    {
        var normalized = name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, "请输入商品名称");
        }

        if (normalized.Length > NameMaxLength)
        {
            throw new BusinessException(400, $"商品名称不能超过 {NameMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeDescription(string? description)
    {
        var normalized = description?.Trim() ?? string.Empty;

        if (normalized.Length > DescriptionMaxLength)
        {
            throw new BusinessException(400, $"商品描述不能超过 {DescriptionMaxLength} 个字符");
        }

        return normalized;
    }

    /// <summary>
    /// 确认商品编码未被其他商品占用。
    /// </summary>
    public static async Task EnsureCodeNotDuplicatedAsync(
        IMallDbContext dbContext,
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var duplicated = await dbContext.MallProducts.AnyAsync(
            product =>
                !product.IsDeleted &&
                product.Code == code &&
                (!excludeId.HasValue || product.Id != excludeId.Value),
            cancellationToken);

        if (duplicated)
        {
            throw new BusinessException(400, "商品编码已存在");
        }
    }

    public static string NormalizeSkuCode(string? skuCode)
    {
        var normalized = skuCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, "请输入规格编码");
        }

        if (normalized.Length > SkuCodeMaxLength)
        {
            throw new BusinessException(400, $"规格编码不能超过 {SkuCodeMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeSpecName(string? specName)
    {
        var normalized = specName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, "请输入规格名称");
        }

        if (normalized.Length > SpecNameMaxLength)
        {
            throw new BusinessException(400, $"规格名称不能超过 {SpecNameMaxLength} 个字符");
        }

        return normalized;
    }

    public static void ValidatePrice(decimal price)
    {
        if (price <= 0)
        {
            throw new BusinessException(400, "规格价格必须大于 0");
        }
    }

    /// <summary>
    /// 市场价是划线展示价，允许不设置（0），设置时不能为负数。
    /// </summary>
    public static void ValidateMarketPrice(decimal marketPrice)
    {
        if (marketPrice < 0)
        {
            throw new BusinessException(400, "规格市场价不能小于 0");
        }
    }

    public static void ValidateWeight(int weightGram)
    {
        if (weightGram <= 0)
        {
            throw new BusinessException(400, "规格重量必须大于 0");
        }

        if (weightGram > 1000000)
        {
            throw new BusinessException(400, "规格重量不能超过 1000000 克");
        }
    }

    public static string NormalizeMainImageUrl(string? mainImageUrl)
    {
        var normalized = mainImageUrl?.Trim() ?? string.Empty;

        if (normalized.Length > ImageUrlMaxLength)
        {
            throw new BusinessException(400, $"主图地址不能超过 {ImageUrlMaxLength} 个字符");
        }

        return normalized;
    }

    public static List<string> NormalizeAlbumImages(List<string>? albumImages)
    {
        var normalized = (albumImages ?? new List<string>())
            .Select(image => image?.Trim() ?? string.Empty)
            .Where(image => !string.IsNullOrWhiteSpace(image))
            .ToList();

        if (normalized.Count > MaxAlbumImages)
        {
            throw new BusinessException(400, $"相册最多上传 {MaxAlbumImages} 张图片");
        }

        if (normalized.Any(image => image.Length > ImageUrlMaxLength))
        {
            throw new BusinessException(400, $"图片地址不能超过 {ImageUrlMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeDetailHtml(string? detailHtml)
    {
        var normalized = detailHtml?.Trim() ?? string.Empty;

        if (normalized.Length > DetailHtmlMaxLength)
        {
            throw new BusinessException(400, $"商品详情不能超过 {DetailHtmlMaxLength} 个字符");
        }

        return normalized;
    }

    public static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0 || sortOrder > MaxSortOrder)
        {
            throw new BusinessException(400, $"排序值必须在 0 到 {MaxSortOrder} 之间");
        }
    }

    /// <summary>
    /// 相册图片序列化为 JSON 字符串。
    /// </summary>
    public static string SerializeAlbumImages(List<string> albumImages)
    {
        return albumImages.Count == 0 ? string.Empty : JsonSerializer.Serialize(albumImages);
    }

    /// <summary>
    /// 解析相册图片 JSON，历史数据或异常数据按空列表处理。
    /// </summary>
    public static List<string> DeserializeAlbumImages(string? albumImages)
    {
        if (string.IsNullOrWhiteSpace(albumImages))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(albumImages) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// 确认分类存在且启用。
    /// </summary>
    public static async Task EnsureCategoryValidAsync(
        IMallDbContext dbContext,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue)
        {
            throw new BusinessException(400, "请选择商品末级分类");
        }

        var category = await dbContext.MallCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == categoryId.Value && !item.IsDeleted, cancellationToken);

        if (category is null)
        {
            throw new BusinessException(404, "商品分类不存在");
        }

        if (!category.IsActive)
        {
            throw new BusinessException(400, "商品分类已停用");
        }

        if (category.ParentId.HasValue)
        {
            var parentIsActive = await dbContext.MallCategories.AnyAsync(
                item => item.Id == category.ParentId.Value &&
                        item.StoreId == category.StoreId &&
                        item.IsActive &&
                        !item.IsDeleted,
                cancellationToken);
            if (!parentIsActive)
            {
                throw new BusinessException(400, "商品分类的父级分类已停用");
            }
        }

        var hasChildren = await dbContext.MallCategories
            .AnyAsync(item => item.ParentId == categoryId.Value && !item.IsDeleted, cancellationToken);
        if (hasChildren)
        {
            throw new BusinessException(400, "商品必须归属末级分类");
        }
    }

    public static async Task<List<MallTag>> GetActiveTagsAsync(
        IMallDbContext dbContext,
        IEnumerable<Guid>? tagIds,
        CancellationToken cancellationToken)
    {
        var ids = tagIds?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (ids.Length == 0) return new List<MallTag>();

        var tags = await dbContext.MallTags
            .Where(tag => ids.Contains(tag.Id) && tag.IsActive && !tag.IsDeleted)
            .ToListAsync(cancellationToken);
        if (tags.Count != ids.Length)
        {
            throw new BusinessException(400, "所选商品标签不存在或已停用");
        }

        return tags;
    }

    public static void ValidateStock(int stock)
    {
        if (stock < 0)
        {
            throw new BusinessException(400, "库存不能小于 0");
        }

        if (stock > 1000000)
        {
            throw new BusinessException(400, "库存不能超过 1000000");
        }
    }

    public static void ValidateWarnStock(int warnStock)
    {
        if (warnStock < 0)
        {
            throw new BusinessException(400, "预警库存不能小于 0");
        }

        if (warnStock > 1000000)
        {
            throw new BusinessException(400, "预警库存不能超过 1000000");
        }
    }

    /// <summary>
    /// 确认规格编码未被其他规格占用。
    /// </summary>
    public static async Task EnsureSkuCodeNotDuplicatedAsync(
        IMallDbContext dbContext,
        string skuCode,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var duplicated = await dbContext.MallSkus.AnyAsync(
            sku =>
                !sku.IsDeleted &&
                sku.SkuCode == skuCode &&
                (!excludeId.HasValue || sku.Id != excludeId.Value),
            cancellationToken);

        if (duplicated)
        {
            throw new BusinessException(400, "规格编码已存在");
        }
    }
}
