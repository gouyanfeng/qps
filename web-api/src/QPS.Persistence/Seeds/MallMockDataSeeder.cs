using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QPS.Mall.Domain.Entities;
using QPS.Persistence.Database;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace QPS.Persistence.Seeds;

/// <summary>
/// 商城种子数据：与系统基线数据一致，仅在商城为空（首次运行）时自动写入。
/// 数据以嵌入资源随程序集发布，不依赖源码目录，生产环境同样生效。
/// </summary>
public static class MallMockDataSeeder
{
    private const string SeedResourcePrefix = "QPS.Persistence.Seeds.MallSeed.";

    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        // 首次运行判定：已有门店或分类说明商城已初始化过，直接跳过，避免覆盖运营维护的数据
        var initialized = await db.MallStores.IgnoreQueryFilters().AnyAsync(ct)
            || await db.MallCategories.IgnoreQueryFilters().AnyAsync(ct);
        if (initialized)
        {
            logger.LogInformation("商城已有数据，跳过种子数据初始化。");
            return;
        }

        var categoriesJson = ReadSeedJson("categories.json");
        var bannersJson = ReadSeedJson("banners.json");
        var promotionsJson = ReadSeedJson("promotions.json");
        var productsJson = ReadSeedJson("products.json");
        var tagsJson = ReadSeedJson("tags.json");
        var freightJson = ReadSeedJson("freight-templates.json");
        var couponsJson = ReadSeedJson("coupons.json");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var freightTemplateId = SeedFreightTemplates(db, freightJson);
        var storeId = await EnsureStoreAsync(db, freightTemplateId, ct);

        var existingCategories = await db.MallCategories
            .IgnoreQueryFilters()
            .Where(category => category.Code.StartsWith("MOCK-CAT-"))
            .ToDictionaryAsync(category => category.Code, ct);
        var newCategories = new List<MallCategory>();
        var categories = ParseCategories(categoriesJson, existingCategories, newCategories);
        db.MallCategories.AddRange(newCategories);
        var sourceCategoryCodes = categories.Select(category => category.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var category in existingCategories.Values.Where(category => !sourceCategoryCodes.Contains(category.Code)))
        {
            category.ChangeStatus(false);
        }

        var existingProducts = await db.MallProducts
            .IgnoreQueryFilters()
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
        var (products, skus, skuIdsByMockId) = ParseProducts(
            productsJson,
            storeId,
            categoryIdsByMockId,
            existingProductsByCode,
            existingSkusByCode,
            newProducts,
            newSkus,
            sourceProductCodes,
            sourceSkuCodes);
        var tagSync = await SyncTagsAsync(db, tagsJson, products, existingProducts, ct);
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

        var productIdsByMockId = products.ToDictionary(
            product => product.Code["MOCK-PROD-".Length..],
            product => product.Id,
            StringComparer.Ordinal);
        var promotionIdsByMockId = await SyncPromotionsAsync(
            db,
            promotionsJson,
            productIdsByMockId,
            categoryIdsByMockId,
            skuIdsByMockId,
            ct);
        var banners = ParseBanners(
            bannersJson,
            productIdsByMockId,
            categoryIdsByMockId);
        var existingBanners = await db.MallBanners
            .IgnoreQueryFilters()
            .ToListAsync(ct);
        var newBanners = new List<MallBanner>();
        var nextSortOrderByPosition = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var banner in banners)
        {
            var existing = existingBanners.FirstOrDefault(current =>
                current.Position == banner.Position &&
                current.Title == banner.Title &&
                current.ImageUrl == banner.ImageUrl);
            if (existing is null)
            {
                if (!nextSortOrderByPosition.TryGetValue(banner.Position, out var nextSortOrder))
                {
                    nextSortOrder = existingBanners
                        .Where(current => current.Position == banner.Position)
                        .Select(current => current.SortOrder)
                        .DefaultIfEmpty(0)
                        .Max() + 1;
                }

                banner.SetSortOrder(nextSortOrder);
                nextSortOrderByPosition[banner.Position] = nextSortOrder + 1;
                newBanners.Add(banner);
                continue;
            }

            existing.Update(
                banner.Position,
                banner.ImageUrl,
                banner.LinkType,
                banner.LinkValue,
                banner.Title,
                banner.StartDate,
                banner.EndDate);
            existing.ChangeStatus(true);
            existing.Restore();
        }
        db.MallBanners.AddRange(newBanners);

        var couponCount = SeedCoupons(db, couponsJson);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "✅ 已写入商城种子数据：{CategoryCount} 条分类、{ProductCount} 条商品、{SkuCount} 条 SKU、{PromotionCount} 条促销、{BannerCount} 条 Banner、{CouponCount} 张优惠券",
            categories.Count,
            products.Count,
            skus.Count,
            promotionIdsByMockId.Count,
            banners.Count,
            couponCount);
        logger.LogInformation("✅ 已写入商城种子标签：{TagCount} 个标签、{ProductTagCount} 条商品标签关系",
            tagSync.TagCount,
            tagSync.ProductTagCount);
    }

    private static string ReadSeedJson(string fileName)
    {
        var assembly = typeof(MallMockDataSeeder).Assembly;
        var resourceName = $"{SeedResourcePrefix}{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"商城种子数据资源缺失: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static Guid SeedFreightTemplates(AppDbContext db, string json)
    {
        using var document = JsonDocument.Parse(json);
        Guid? defaultTemplateId = null;

        foreach (var item in document.RootElement.GetProperty("templates").EnumerateArray())
        {
            var template = MallFreightTemplate.Create(
                item.GetProperty("name").GetString()!,
                Enum.Parse<FreightBillingType>(item.GetProperty("billingType").GetString()!, ignoreCase: true),
                item.GetProperty("isDefault").GetBoolean(),
                item.GetProperty("isActive").GetBoolean());
            db.MallFreightTemplates.Add(template);
            if (template.IsDefault)
            {
                defaultTemplateId ??= template.Id;
            }

            foreach (var region in item.GetProperty("regions").EnumerateArray())
            {
                db.MallFreightRegions.Add(MallFreightRegion.Create(
                    template.Id,
                    region.GetProperty("regionName").GetString()!,
                    region.TryGetProperty("provinceNames", out var provinceNames)
                        ? provinceNames.GetString() ?? string.Empty
                        : string.Empty,
                    region.TryGetProperty("cityNames", out var cityNames)
                        ? cityNames.GetString()
                        : null,
                    region.GetProperty("firstValue").GetInt32(),
                    region.GetProperty("firstFee").GetDecimal(),
                    region.GetProperty("stepValue").GetInt32(),
                    region.GetProperty("stepFee").GetDecimal(),
                    region.TryGetProperty("freeShippingAmount", out var freeShippingAmount) &&
                    freeShippingAmount.ValueKind != JsonValueKind.Null
                        ? freeShippingAmount.GetDecimal()
                        : null,
                    region.TryGetProperty("freeShippingQuantity", out var freeShippingQuantity) &&
                    freeShippingQuantity.ValueKind != JsonValueKind.Null
                        ? freeShippingQuantity.GetInt32()
                        : null,
                    region.GetProperty("isDefaultRegion").GetBoolean()));
            }
        }

        return defaultTemplateId
            ?? throw new InvalidDataException("商城种子数据缺少默认运费模板");
    }

    private static int SeedCoupons(AppDbContext db, string json)
    {
        using var document = JsonDocument.Parse(json);
        var count = 0;

        foreach (var item in document.RootElement.GetProperty("coupons").EnumerateArray())
        {
            var coupon = MallCoupon.Create(
                item.GetProperty("code").GetString()!,
                item.GetProperty("name").GetString()!,
                item.GetProperty("couponType").GetString()!,
                item.GetProperty("thresholdAmount").GetDecimal(),
                item.GetProperty("discountAmount").GetDecimal(),
                item.TryGetProperty("maxDiscount", out var maxDiscount) &&
                maxDiscount.ValueKind != JsonValueKind.Null
                    ? maxDiscount.GetDecimal()
                    : null,
                item.GetProperty("totalCount").GetInt32(),
                item.GetProperty("perLimit").GetInt32(),
                item.GetProperty("validDays").GetInt32(),
                null,
                null,
                item.TryGetProperty("scopeType", out var scopeType)
                    ? scopeType.GetString() ?? "all"
                    : "all",
                null);
            db.MallCoupons.Add(coupon);
            count++;
        }

        return count;
    }

    private static async Task<Guid> EnsureStoreAsync(AppDbContext db, Guid freightTemplateId, CancellationToken ct)
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
            isActive: true,
            defaultFreightTemplateId: freightTemplateId);
        db.MallStores.Add(store);
        await db.SaveChangesAsync(ct);
        return store.Id;
    }

    private static List<MallCategory> ParseCategories(
        string json,
        Dictionary<string, MallCategory> existingByCode,
        List<MallCategory> additions)
    {
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
                parentCode, parentName, null, parentIcon, sort, existingByCode, additions);
            result.Add(parentCategory);
            sort++;

            if (parent.TryGetProperty("children", out var children))
            {
                int childSort = 1;
                foreach (var child in children.EnumerateArray())
                {
                    var childCode = $"MOCK-CAT-{child.GetProperty("id").GetString()}";
                    var childCategory = GetOrCreateCategory(
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
            // 种子未提供图标时保留后台已维护的图标，避免启动时覆盖上传结果
            existing.Update(code, name, parentId, icon ?? existing.Icon, sortOrder);
            existing.ChangeStatus(true);
            existing.Restore();
            return existing;
        }

        var category = MallCategory.Create(code, name, parentId, icon, sortOrder, true);
        existingByCode.Add(code, category);
        additions.Add(category);
        return category;
    }

    private static async Task<Dictionary<string, Guid>> SyncPromotionsAsync(
        AppDbContext db,
        string json,
        IReadOnlyDictionary<string, Guid> productIdsByMockId,
        IReadOnlyDictionary<string, Guid> categoryIdsByMockId,
        IReadOnlyDictionary<string, Guid> skuIdsByMockId,
        CancellationToken ct)
    {
        var existingPromotions = await db.MallPromotions
            .IgnoreQueryFilters()
            .Where(promotion => promotion.Name.StartsWith("[测试]"))
            .ToListAsync(ct);
        var promotionsByName = existingPromotions.ToDictionary(promotion => promotion.Name, StringComparer.Ordinal);
        var promotionIdsByMockId = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var sourcePromotionNames = new HashSet<string>(StringComparer.Ordinal);
        using var document = JsonDocument.Parse(json);

        foreach (var item in document.RootElement.GetProperty("promotions").EnumerateArray())
        {
            var mockId = item.GetProperty("id").GetString()
                ?? throw new InvalidDataException("测试促销缺少 id");
            var name = item.GetProperty("name").GetString()
                ?? throw new InvalidDataException($"测试促销 {mockId} 缺少 name");
            var description = item.TryGetProperty("description", out var descriptionElement)
                ? descriptionElement.GetString() ?? string.Empty
                : string.Empty;
            var thresholdAmount = item.TryGetProperty("thresholdAmount", out var thresholdElement)
                ? thresholdElement.GetDecimal()
                : 0m;
            var actionType = item.GetProperty("actionType").GetString()!;
            var actionConfigJson = ResolvePromotionConfig(
                item.GetProperty("actionConfigJson").GetString()!,
                productIdsByMockId,
                skuIdsByMockId);

            // 从 actionType 映射 PromotionType 枚举
            var promotionType = actionType switch
            {
                "DiscountAmount" => MallPromotionType.DiscountAmount,
                "DiscountRate" => MallPromotionType.DiscountRate,
                "Gift" => MallPromotionType.Gift,
                _ => MallPromotionType.DiscountAmount
            };
            var scopeType = item.TryGetProperty("scopeType", out var scopeElement)
                ? scopeElement.GetString() ?? "Global"
                : "Global";
            var isActive = !item.TryGetProperty("isActive", out var activeElement) || activeElement.GetBoolean();
            sourcePromotionNames.Add(name);

            if (!promotionsByName.TryGetValue(name, out var promotion))
            {
                promotion = MallPromotion.Create(
                    name,
                    description,
                    promotionType,
                    thresholdAmount,
                    actionType,
                    actionConfigJson,
                    scopeType,
                    promotionTagId: null,
                    startDate: null,
                    endDate: null,
                    isActive: isActive);
                db.MallPromotions.Add(promotion);
                promotionsByName.Add(name, promotion);
            }
            else
            {
                promotion.Update(
                    name,
                    description,
                    promotionType,
                    thresholdAmount,
                    actionType,
                    actionConfigJson,
                    scopeType,
                    promotion.PromotionTagId,
                    startDate: null,
                    endDate: null);
                promotion.ChangeStatus(isActive);
                promotion.Restore();
            }

            promotionIdsByMockId.Add(mockId, promotion.Id);
        }

        foreach (var promotion in existingPromotions.Where(promotion => !sourcePromotionNames.Contains(promotion.Name)))
        {
            promotion.ChangeStatus(false);
        }

        await db.SaveChangesAsync(ct);

        var managedPromotionIds = promotionIdsByMockId.Values.ToArray();
        var existingScopes = await db.MallPromotionScopes
            .IgnoreQueryFilters()
            .Where(scope => managedPromotionIds.Contains(scope.PromotionId))
            .ToListAsync(ct);
        var existingScopesByKey = existingScopes
            .GroupBy(scope => (scope.PromotionId, scope.ScopeTargetType, scope.ScopeTargetId))
            .ToDictionary(group => group.Key, group => group.First());
        foreach (var scope in existingScopes)
        {
            scope.SoftDelete();
        }

        using var scopesDocument = JsonDocument.Parse(json);
        foreach (var item in scopesDocument.RootElement.GetProperty("promotions").EnumerateArray())
        {
            var mockId = item.GetProperty("id").GetString()!;
            var promotionId = promotionIdsByMockId[mockId];
            var scopeType = item.TryGetProperty("scopeType", out var scopeElement)
                ? scopeElement.GetString() ?? "Global"
                : "Global";
            if (scopeType == "Global" || !item.TryGetProperty("scopeTargetIds", out var targetsElement))
            {
                continue;
            }

            foreach (var targetId in targetsElement.EnumerateArray().Select(element => element.GetString()!).Distinct())
            {
                var target = ResolveScopeTargetId(
                    scopeType,
                    targetId,
                    productIdsByMockId,
                    categoryIdsByMockId,
                    skuIdsByMockId,
                    item.GetProperty("name").GetString()!);
                var key = (promotionId, scopeType, target);
                if (existingScopesByKey.TryGetValue(key, out var existingScope))
                {
                    existingScope.Restore();
                }
                else
                {
                    var newScope = MallPromotionScope.Create(promotionId, scopeType, target);
                    db.MallPromotionScopes.Add(newScope);
                    existingScopesByKey.Add(key, newScope);
                }
            }
        }

        await db.SaveChangesAsync(ct);
        return promotionIdsByMockId;
    }

    private static string ResolvePromotionConfig(
        string configJson,
        IReadOnlyDictionary<string, Guid> productIdsByMockId,
        IReadOnlyDictionary<string, Guid> skuIdsByMockId)
    {
        var config = JsonNode.Parse(configJson)
            ?? throw new InvalidDataException("测试促销配置不能为空");
        if (config is JsonObject configObject && configObject["productIds"] is JsonArray productIds)
        {
            for (var index = 0; index < productIds.Count; index++)
            {
                var mockId = productIds[index]?.GetValue<string>()
                    ?? throw new InvalidDataException("测试促销商品 ID 无效");
                if (!productIdsByMockId.TryGetValue(mockId, out var productId))
                {
                    throw new InvalidDataException($"测试促销引用了不存在的商品: {mockId}");
                }

                productIds[index] = productId.ToString();
            }
        }

        if (config is JsonObject actionObject && actionObject["skuId"] is JsonValue skuElement)
        {
            var mockId = skuElement.GetValue<string>();
            if (!skuIdsByMockId.TryGetValue(mockId, out var skuId))
            {
                throw new InvalidDataException($"测试促销引用了不存在的 SKU: {mockId}");
            }

            actionObject["skuId"] = skuId.ToString();
        }

        return config.ToJsonString();
    }

    private static Guid ResolveScopeTargetId(
        string scopeType,
        string mockId,
        IReadOnlyDictionary<string, Guid> productIdsByMockId,
        IReadOnlyDictionary<string, Guid> categoryIdsByMockId,
        IReadOnlyDictionary<string, Guid> skuIdsByMockId,
        string promotionName)
    {
        var targets = scopeType switch
        {
            "Product" => productIdsByMockId,
            "Category" => categoryIdsByMockId,
            "Sku" => skuIdsByMockId,
            _ => throw new InvalidDataException($"测试促销 {promotionName} 的适用范围无效: {scopeType}")
        };
        if (!targets.TryGetValue(mockId, out var targetId))
        {
            throw new InvalidDataException($"测试促销 {promotionName} 引用了不存在的 {scopeType} 对象: {mockId}");
        }

        return targetId;
    }

    private static List<MallBanner> ParseBanners(
        string json,
        IReadOnlyDictionary<string, Guid> productIdsByMockId,
        IReadOnlyDictionary<string, Guid> categoryIdsByMockId)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.GetProperty("banners");

        var result = new List<MallBanner>();
        int sort = 1;

        foreach (var item in root.EnumerateArray())
        {
            var position = item.TryGetProperty("position", out var positionElement)
                ? positionElement.GetString() ?? "home"
                : "home";
            var image = item.GetProperty("image").GetString()!;
            var title = item.GetProperty("title").GetString()!;
            var link = item.TryGetProperty("link", out var l) ? l.GetString() ?? string.Empty : string.Empty;
            var linkType = MallBannerLinkType.ExternalUrl;
            if (item.TryGetProperty("linkType", out var linkTypeElement))
            {
                var linkTypeName = linkTypeElement.GetString();
                if (!Enum.TryParse(linkTypeName, ignoreCase: true, out linkType))
                {
                    throw new InvalidDataException($"Banner {title} 的跳转类型无效: {linkTypeName}");
                }
            }

            if (linkType == MallBannerLinkType.InternalProduct)
            {
                var productMockId = item.TryGetProperty("productId", out var productElement)
                    ? productElement.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(productMockId) ||
                    !productIdsByMockId.TryGetValue(productMockId, out var productId))
                {
                    throw new InvalidDataException($"Banner {title} 引用了不存在的测试商品: {productMockId}");
                }

                link = productId.ToString();
            }
            else if (linkType == MallBannerLinkType.InternalCategory)
            {
                var categoryMockId = item.TryGetProperty("categoryId", out var categoryElement)
                    ? categoryElement.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(categoryMockId) ||
                    !categoryIdsByMockId.TryGetValue(categoryMockId, out var categoryId))
                {
                    throw new InvalidDataException($"Banner {title} 引用了不存在的测试分类: {categoryMockId}");
                }

                link = categoryId.ToString();
            }

            result.Add(MallBanner.Create(
                position: position,
                imageUrl: image,
                linkType,
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

    private static (
        List<MallProduct> products,
        List<MallSku> skus,
        Dictionary<string, Guid> skuIdsByMockId) ParseProducts(
        string json,
        Guid storeId,
        Dictionary<string, Guid> categoryIdsByMockId,
        Dictionary<string, MallProduct> existingProductsByCode,
        Dictionary<string, MallSku> existingSkusByCode,
        List<MallProduct> newProducts,
        List<MallSku> newSkus,
        HashSet<string> sourceProductCodes,
        HashSet<string> sourceSkuCodes)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.GetProperty("products");

        var products = new List<MallProduct>();
        var skus = new List<MallSku>();
        var skuIdsByMockId = new Dictionary<string, Guid>(StringComparer.Ordinal);

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
                // 首页精选属于后台运营配置，已存在商品不按种子覆盖
                product.Update(
                    existingProduct.StoreId == Guid.Empty ? storeId : existingProduct.StoreId,
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
                product.Restore();
            }
            else
            {
                product = MallProduct.Create(
                    storeId: storeId,
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
                    var mockSkuId = sku.GetProperty("id").GetString()
                        ?? throw new InvalidDataException($"商品 {name} 的 SKU 缺少 id");
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
                        existingSku.Restore();
                        skus.Add(existingSku);
                    }
                    else
                    {
                        var newSku = MallSku.Create(product.Id, skuCode, specName, price, weightGram, stock, 5, true);
                        newSkus.Add(newSku);
                        existingSkusByCode.Add(skuCode, newSku);
                        skus.Add(newSku);
                    }
                    skuIdsByMockId.Add(mockSkuId, existingSkusByCode[skuCode].Id);
                    sourceSkuCodes.Add(skuCode);
                    skuIdx++;
                }
            }
        }

        return (products, skus, skuIdsByMockId);
    }

    private static async Task<(int TagCount, int ProductTagCount)> SyncTagsAsync(
        AppDbContext db,
        string json,
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

        using var document = JsonDocument.Parse(json);
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
                tag.Restore();
            }
            else
            {
                tag = MallTag.Create(code, name, color, sortOrder, true, MallTagType.Product);
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
        var mockProductIds = mockProducts.Select(p => p.Id).ToArray();

        var existingProductTags = mockProductIds.Length == 0
            ? new List<MallProductTag>()
            : await db.MallProductTags
                .IgnoreQueryFilters()
                .Where(link => mockProductIds.Contains(link.ProductId))
                .ToListAsync(ct);

        var tagCodeById = existingTags.Values.ToDictionary(t => t.Id, t => t.Code);
        foreach (var link in existingProductTags)
        {
            if (!link.IsDeleted && tagCodeById.TryGetValue(link.TagId, out var code) && code.StartsWith("MOCK-TAG-", StringComparison.Ordinal))
            {
                link.SoftDelete();
            }
        }

        await db.SaveChangesAsync(ct);

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

                db.MallProductTags.Add(MallProductTag.Create(product.Id, tag.Id));
                relationCount++;
            }
        }

        return (tagsByCode.Count, relationCount);
    }
}