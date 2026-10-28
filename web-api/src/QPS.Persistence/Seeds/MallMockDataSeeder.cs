using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QPS.Mall.Domain.Entities;
using QPS.Persistence.Database;
using System.Text.Json;

namespace QPS.Persistence.Seeds;

public static class MallMockDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger, string mockRootPath, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var storeId = await EnsureStoreAsync(db, ct);
        logger.LogInformation("🌱 MallMockDataSeeder using StoreId = {StoreId}", storeId);

        var categoriesPath = Path.Combine(mockRootPath, "categories.json");
        var bannersPath = Path.Combine(mockRootPath, "banners.json");
        var productsPath = Path.Combine(mockRootPath, "products.json");
        var tagsPath = Path.Combine(mockRootPath, "tags.json");
        if (!File.Exists(categoriesPath) || !File.Exists(bannersPath) ||
            !File.Exists(productsPath) || !File.Exists(tagsPath))
        {
            throw new FileNotFoundException($"商城 mock 数据文件不完整: {mockRootPath}");
        }

        var existingCategories = await db.MallCategories
            .IgnoreQueryFilters()
            .Where(category => category.StoreId == storeId && category.Code.StartsWith("MOCK-CAT-"))
            .ToDictionaryAsync(category => category.Code, ct);
        var newCategories = new List<MallCategory>();
        var categories = ParseCategories(categoriesPath, storeId, existingCategories, newCategories);
        db.MallCategories.AddRange(newCategories);
        var sourceCategoryCodes = categories.Select(category => category.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var category in existingCategories.Values.Where(category => !sourceCategoryCodes.Contains(category.Code)))
        {
            category.ChangeStatus(false);
        }

        var existingProducts = await db.MallProducts
            .IgnoreQueryFilters()
            .Include(product => product.Tags)
            .Where(product => product.Code.StartsWith("MOCK-PROD-"))
            .ToListAsync(ct);
        var existingProductsByCode = existingProducts.ToDictionary(product => product.Code);
        var productIds = existingProducts.Select(product => product.Id).ToArray();
        var existingSkus = await db.MallSkus
            .IgnoreQueryFilters()
            .Where(sku => productIds.Contains(sku.ProductId))
            .ToListAsync(ct);
        var existingSkusByCode = existingSkus.ToDictionary(sku => sku.SkuCode);
        var newProducts = new List<MallProduct>();
        var newSkus = new List<MallSku>();
        var sourceProductCodes = new HashSet<string>(StringComparer.Ordinal);
        var sourceSkuCodes = new HashSet<string>(StringComparer.Ordinal);
        var categoryIdsByMockId = categories.ToDictionary(
            category => category.Code["MOCK-CAT-".Length..],
            category => category.Id,
            StringComparer.Ordinal);
        var (products, skus) = ParseProducts(
            productsPath,
            categoryIdsByMockId,
            existingProductsByCode,
            existingSkusByCode,
            newProducts,
            newSkus,
            sourceProductCodes,
            sourceSkuCodes);
        var tagSync = await SyncTagsAsync(db, tagsPath, products, existingProducts, ct);
        db.MallProducts.AddRange(newProducts);
        db.MallSkus.AddRange(newSkus);
        foreach (var product in existingProducts.Where(product => !sourceProductCodes.Contains(product.Code)))
        {
            product.ChangeStatus(false);
            foreach (var sku in existingSkus.Where(sku => sku.ProductId == product.Id))
            {
                sku.ChangeStatus(false);
            }
        }
        foreach (var sku in existingSkus.Where(sku => !sourceSkuCodes.Contains(sku.SkuCode)))
        {
            sku.ChangeStatus(false);
        }

        var banners = ParseBanners(bannersPath, storeId);
        var existingBanners = await db.MallBanners
            .IgnoreQueryFilters()
            .Where(banner => banner.StoreId == storeId && banner.Position == "home")
            .ToListAsync(ct);
        var newBanners = new List<MallBanner>();
        foreach (var banner in banners)
        {
            var existing = existingBanners.FirstOrDefault(current =>
                current.Title == banner.Title && current.ImageUrl == banner.ImageUrl);
            if (existing is null)
            {
                newBanners.Add(banner);
                continue;
            }

            existing.Update(
                "home",
                banner.ImageUrl,
                banner.LinkType,
                banner.LinkValue,
                banner.Title,
                banner.StartDate,
                banner.EndDate);
            existing.SetSortOrder(banner.SortOrder);
            existing.ChangeStatus(true);
            existing.IsDeleted = false;
        }
        db.MallBanners.AddRange(newBanners);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "✅ 已同步商城 mock 数据：{CategoryCount} 条分类、{ProductCount} 条商品、{SkuCount} 条 SKU、{BannerCount} 条轮播",
            categories.Count,
            products.Count,
            skus.Count,
            banners.Count);
        logger.LogInformation("✅ 已同步商城 mock 标签：{TagCount} 个标签、{ProductTagCount} 条商品标签关系",
            tagSync.TagCount,
            tagSync.ProductTagCount);
    }

    private static async Task<Guid> EnsureStoreAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = await db.MallStores
            .Where(store => store.IsActive)
            .OrderByDescending(store => store.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (existing is not null) return existing.Id;

        var store = MallStore.Create(
            code: "STORE-001",
            name: "本草堂旗舰店",
            address: "浙江省杭州市西湖区文三路 123 号",
            isActive: true);
        db.MallStores.Add(store);
        await db.SaveChangesAsync(ct);
        return store.Id;
    }

    private static List<MallCategory> ParseCategories(
        string path,
        Guid storeId,
        Dictionary<string, MallCategory> existingByCode,
        List<MallCategory> additions)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.GetProperty("categories");

        var result = new List<MallCategory>();
        int sort = 1;

        foreach (var parent in root.EnumerateArray())
        {
            var parentName = parent.GetProperty("name").GetString()!;
            var parentIcon = parent.TryGetProperty("icon", out var pIcon) ? pIcon.GetString() : null;

            var parentCode = $"MOCK-CAT-{parent.GetProperty("id").GetString()}";
            var parentCategory = GetOrCreateCategory(
                storeId, parentCode, parentName, null, parentIcon, sort, existingByCode, additions);
            result.Add(parentCategory);
            sort++;

            if (parent.TryGetProperty("children", out var children))
            {
                int childSort = 1;
                foreach (var child in children.EnumerateArray())
                {
                    var childCode = $"MOCK-CAT-{child.GetProperty("id").GetString()}";
                    var childCategory = GetOrCreateCategory(
                        storeId,
                        childCode,
                        child.GetProperty("name").GetString()!,
                        parentCategory.Id,
                        null,
                        childSort,
                        existingByCode,
                        additions);
                    result.Add(childCategory);
                    sort++;
                    childSort++;
                }
            }
        }

        return result;
    }

    private static MallCategory GetOrCreateCategory(
        Guid storeId,
        string code,
        string name,
        Guid? parentId,
        string? icon,
        int sortOrder,
        Dictionary<string, MallCategory> existingByCode,
        List<MallCategory> additions)
    {
        if (existingByCode.TryGetValue(code, out var existing))
        {
            // mock 数据未提供图标时保留后台已维护的图标，避免每次启动覆盖上传结果
            existing.Update(code, name, parentId, icon ?? existing.Icon, sortOrder);
            existing.ChangeStatus(true);
            existing.IsDeleted = false;
            return existing;
        }

        var category = MallCategory.Create(storeId, code, name, parentId, icon, sortOrder, true);
        existingByCode.Add(code, category);
        additions.Add(category);
        return category;
    }

    private static List<MallBanner> ParseBanners(string path, Guid storeId)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.GetProperty("banners");

        var result = new List<MallBanner>();
        int sort = 1;

        foreach (var item in root.EnumerateArray())
        {
            var image = item.GetProperty("image").GetString()!;
            var title = item.GetProperty("title").GetString()!;
            var link = item.TryGetProperty("link", out var l) ? l.GetString() ?? string.Empty : string.Empty;

            result.Add(MallBanner.Create(
                storeId: storeId,
                position: "home",
                imageUrl: image,
                linkType: string.IsNullOrEmpty(link) ? MallBannerLinkType.ExternalUrl : MallBannerLinkType.ExternalUrl,
                linkValue: link,
                title: title,
                sortOrder: sort,
                startDate: null,
                endDate: null,
                isActive: true));
            sort++;
        }

        return result;
    }

    private static (List<MallProduct> products, List<MallSku> skus) ParseProducts(
        string path,
        Dictionary<string, Guid> categoryIdsByMockId,
        Dictionary<string, MallProduct> existingProductsByCode,
        Dictionary<string, MallSku> existingSkusByCode,
        List<MallProduct> newProducts,
        List<MallSku> newSkus,
        HashSet<string> sourceProductCodes,
        HashSet<string> sourceSkuCodes)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.GetProperty("products");

        var products = new List<MallProduct>();
        var skus = new List<MallSku>();

        foreach (var item in root.EnumerateArray())
        {
            var name = item.GetProperty("name").GetString()!;
            var description = item.GetProperty("description").GetString()!;
            var sales = item.TryGetProperty("sales", out var s) ? s.GetInt32() : 0;
            var isHot = item.TryGetProperty("isHot", out var h) && h.GetBoolean();

            var mockCategoryId = item.TryGetProperty("categoryId", out var categoryElement)
                ? categoryElement.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(mockCategoryId) ||
                !categoryIdsByMockId.TryGetValue(mockCategoryId, out var categoryIdValue))
            {
                throw new InvalidDataException($"商品 {item.GetProperty("id").GetString()} 的 categoryId 未映射到 mock 分类");
            }
            Guid? categoryId = categoryIdValue;

            string mainImage = string.Empty;
            var albumList = new List<string>();
            if (item.TryGetProperty("images", out var imgs))
            {
                int idx = 0;
                foreach (var img in imgs.EnumerateArray())
                {
                    var url = img.GetString()!;
                    if (idx == 0) mainImage = url;
                    else albumList.Add(url);
                    idx++;
                }
            }

            var code = $"MOCK-PROD-{item.GetProperty("id").GetString()}";
            MallProduct product;
            if (existingProductsByCode.TryGetValue(code, out var existingProduct))
            {
                product = existingProduct;
                // 首页精选属于后台运营配置，已存在商品不按 mock 种子覆盖
                product.Update(
                    code,
                    name,
                    description,
                    categoryId,
                    mainImage,
                    JsonSerializer.Serialize(albumList),
                    string.Empty,
                    products.Count + 1,
                    product.IsHot,
                    null);
                product.ChangeStatus(true);
                product.IsDeleted = false;
            }
            else
            {
                product = MallProduct.Create(
                    code: code,
                    name: name,
                    description: description,
                    categoryId: categoryId,
                    mainImageUrl: mainImage,
                    albumImages: JsonSerializer.Serialize(albumList),
                    detailHtml: string.Empty,
                    sortOrder: products.Count + 1,
                    isActive: true,
                    isHot: isHot);
                product.AddSales(sales);
                newProducts.Add(product);
                existingProductsByCode.Add(code, product);
            }
            sourceProductCodes.Add(code);
            products.Add(product);

            using (var skuJson = JsonDocument.Parse(item.GetProperty("skus").GetRawText()))
            {
                int skuIdx = 1;
                foreach (var sku in skuJson.RootElement.EnumerateArray())
                {
                    var skuCode = $"{code}-SKU{skuIdx}";
                    var specName = sku.GetProperty("specName").GetString()!;
                    var price = sku.GetProperty("price").GetDecimal();
                    var weightGram = sku.TryGetProperty("weightGram", out var wg) ? wg.GetInt32() : 0;
                    var stock = sku.TryGetProperty("stock", out var st) ? st.GetInt32() : 0;
                    if (existingSkusByCode.TryGetValue(skuCode, out var existingSku))
                    {
                        // 市场价由后台维护，种子同步时保留已有值
                        existingSku.Update(skuCode, specName, price, weightGram, stock, 5, marketPrice: existingSku.MarketPrice);
                        existingSku.ChangeStatus(true);
                        existingSku.IsDeleted = false;
                        skus.Add(existingSku);
                    }
                    else
                    {
                        var newSku = MallSku.Create(product.Id, skuCode, specName, price, weightGram, stock, 5, true);
                        newSkus.Add(newSku);
                        existingSkusByCode.Add(skuCode, newSku);
                        skus.Add(newSku);
                    }
                    sourceSkuCodes.Add(skuCode);
                    skuIdx++;
                }
            }
        }

        return (products, skus);
    }

    private static async Task<(int TagCount, int ProductTagCount)> SyncTagsAsync(
        AppDbContext db,
        string path,
        List<MallProduct> products,
        List<MallProduct> existingProducts,
        CancellationToken ct)
    {
        var existingTags = await db.MallTags
            .IgnoreQueryFilters()
            .Where(tag => tag.Code.StartsWith("MOCK-TAG-"))
            .ToDictionaryAsync(tag => tag.Code, StringComparer.Ordinal, ct);
        var tagsByCode = new Dictionary<string, MallTag>(StringComparer.Ordinal);
        var newTags = new List<MallTag>();
        var sourceTagCodes = new HashSet<string>(StringComparer.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var tagElement in document.RootElement.GetProperty("tags").EnumerateArray())
        {
            var code = $"MOCK-TAG-{tagElement.GetProperty("code").GetString()}";
            var name = tagElement.GetProperty("name").GetString()!;
            var color = tagElement.TryGetProperty("color", out var colorElement)
                ? colorElement.GetString() ?? "success"
                : "success";
            var sortOrder = tagElement.TryGetProperty("sortOrder", out var sortElement)
                ? sortElement.GetInt32()
                : 0;
            sourceTagCodes.Add(code);

            if (existingTags.TryGetValue(code, out var tag))
            {
                tag.Update(code, name, color, sortOrder);
                tag.ChangeStatus(true);
                tag.IsDeleted = false;
            }
            else
            {
                tag = MallTag.Create(code, name, color, sortOrder, true);
                existingTags.Add(code, tag);
                newTags.Add(tag);
            }
            tagsByCode.Add(code, tag);
        }

        foreach (var tag in existingTags.Values.Where(tag => !sourceTagCodes.Contains(tag.Code)))
        {
            tag.ChangeStatus(false);
        }

        db.MallTags.AddRange(newTags);
        var productsByCode = products.ToDictionary(product => product.Code, StringComparer.Ordinal);
        var tagCodesByProductId = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var productTag in document.RootElement.GetProperty("productTags").EnumerateArray())
        {
            var productId = productTag.GetProperty("productId").GetString()!;
            var codes = productTag.GetProperty("tagCodes")
                .EnumerateArray()
                .Select(tagCode => $"MOCK-TAG-{tagCode.GetString()}")
                .ToHashSet(StringComparer.Ordinal);
            tagCodesByProductId.Add(productId, codes);
        }

        var mockProducts = existingProducts
            .Concat(products.Where(product => !existingProducts.Contains(product)))
            .ToList();
        foreach (var product in mockProducts)
        {
            foreach (var tag in product.Tags.Where(tag => tag.Code.StartsWith("MOCK-TAG-", StringComparison.Ordinal)).ToList())
            {
                product.Tags.Remove(tag);
            }
        }

        var relationCount = 0;
        foreach (var (mockProductId, tagCodes) in tagCodesByProductId)
        {
            var productCode = $"MOCK-PROD-{mockProductId}";
            if (!productsByCode.TryGetValue(productCode, out var product))
            {
                throw new InvalidDataException($"商品标签引用了不存在的商品: {mockProductId}");
            }

            foreach (var tagCode in tagCodes)
            {
                if (!tagsByCode.TryGetValue(tagCode, out var tag))
                {
                    throw new InvalidDataException($"商品 {mockProductId} 引用了不存在的标签: {tagCode}");
                }

                product.Tags.Add(tag);
                relationCount++;
            }
        }

        return (tagsByCode.Count, relationCount);
    }
}