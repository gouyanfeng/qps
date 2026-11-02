using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QPS.Mall.Domain.Entities;
using QPS.Persistence.Database;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace QPS.Persistence.Seeds;

public static class MallMockDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger, string mockRootPath, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var storeId = await EnsureStoreAsync(db, ct);

        var categoriesPath = Path.Combine(mockRootPath, "categories.json");
        var bannersPath = Path.Combine(mockRootPath, "banners.json");
        var activitiesPath = Path.Combine(mockRootPath, "activities.json");
        var promotionsPath = Path.Combine(mockRootPath, "promotions.json");
        var productsPath = Path.Combine(mockRootPath, "products.json");
        var tagsPath = Path.Combine(mockRootPath, "tags.json");
        if (!File.Exists(categoriesPath) || !File.Exists(bannersPath) ||
            !File.Exists(activitiesPath) || !File.Exists(promotionsPath) ||
            !File.Exists(productsPath) || !File.Exists(tagsPath))
        {
            throw new FileNotFoundException($"商城 mock 数据文件不完整: {mockRootPath}");
        }

        var existingCategories = await db.MallCategories
            .IgnoreQueryFilters()
            .Where(category => category.Code.StartsWith("MOCK-CAT-"))
            .ToDictionaryAsync(category => category.Code, ct);
        var newCategories = new List<MallCategory>();
        var categories = ParseCategories(categoriesPath, existingCategories, newCategories);
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
            productsPath,
            storeId,
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

        var productIdsByMockId = products.ToDictionary(
            product => product.Code["MOCK-PROD-".Length..],
            product => product.Id,
            StringComparer.Ordinal);
        var promotionIdsByMockId = await SyncPromotionsAsync(
            db,
            promotionsPath,
            productIdsByMockId,
            categoryIdsByMockId,
            skuIdsByMockId,
            ct);
        var activityIdsByMockId = await SyncActivitiesAsync(db, activitiesPath, promotionIdsByMockId, ct);
        var banners = ParseBanners(
            bannersPath,
            activityIdsByMockId,
            productIdsByMockId,
            categoryIdsByMockId);
        var existingBanners = await db.MallBanners
            .IgnoreQueryFilters()
            .Where(banner => banner.Position == "home")
            .ToListAsync(ct);
        var newBanners = new List<MallBanner>();
        var nextBannerSortOrder = existingBanners.Count == 0
            ? 1
            : existingBanners.Max(banner => banner.SortOrder) + 1;
        foreach (var banner in banners)
        {
            var existing = existingBanners.FirstOrDefault(current =>
                current.Title == banner.Title && current.ImageUrl == banner.ImageUrl);
            if (existing is null)
            {
                banner.SetSortOrder(nextBannerSortOrder++);
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
            existing.ChangeStatus(true);
            existing.IsDeleted = false;
        }
        db.MallBanners.AddRange(newBanners);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "✅ 已同步商城 mock 数据：{CategoryCount} 条分类、{ProductCount} 条商品、{SkuCount} 条 SKU、{PromotionCount} 条促销、{ActivityCount} 条活动、{BannerCount} 条轮播",
            categories.Count,
            products.Count,
            skus.Count,
            promotionIdsByMockId.Count,
            activityIdsByMockId.Count,
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
            // mock 数据未提供图标时保留后台已维护的图标，避免每次启动覆盖上传结果
            existing.Update(code, name, parentId, icon ?? existing.Icon, sortOrder);
            existing.ChangeStatus(true);
            existing.IsDeleted = false;
            return existing;
        }

        var category = MallCategory.Create(code, name, parentId, icon, sortOrder, true);
        existingByCode.Add(code, category);
        additions.Add(category);
        return category;
    }

    private static async Task<Dictionary<string, Guid>> SyncPromotionsAsync(
        AppDbContext db,
        string path,
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
        var now = DateTime.UtcNow;
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        foreach (var item in document.RootElement.GetProperty("promotions").EnumerateArray())
        {
            var mockId = item.GetProperty("id").GetString()
                ?? throw new InvalidDataException("测试促销缺少 id");
            var name = item.GetProperty("name").GetString()
                ?? throw new InvalidDataException($"测试促销 {mockId} 缺少 name");
            var description = item.TryGetProperty("description", out var descriptionElement)
                ? descriptionElement.GetString() ?? string.Empty
                : string.Empty;
            var triggerType = item.GetProperty("triggerType").GetString()!;
            var triggerConfigJson = ResolvePromotionConfig(
                item.GetProperty("triggerConfigJson").GetString()!,
                productIdsByMockId,
                skuIdsByMockId);
            var actionType = item.GetProperty("actionType").GetString()!;
            var actionConfigJson = ResolvePromotionConfig(
                item.GetProperty("actionConfigJson").GetString()!,
                productIdsByMockId,
                skuIdsByMockId);
            var scopeType = item.TryGetProperty("scopeType", out var scopeElement)
                ? scopeElement.GetString() ?? "Global"
                : "Global";
            var maxUsagePerUser = item.TryGetProperty("maxUsagePerUser", out var perUserElement)
                ? perUserElement.GetInt32()
                : 0;
            var maxUsagePerOrder = item.TryGetProperty("maxUsagePerOrder", out var perOrderElement)
                ? perOrderElement.GetInt32()
                : 0;
            var isActive = !item.TryGetProperty("isActive", out var activeElement) || activeElement.GetBoolean();
            var (startDate, endDate) = ResolveTimeWindow(item, now);
            sourcePromotionNames.Add(name);

            if (!promotionsByName.TryGetValue(name, out var promotion))
            {
                promotion = MallPromotion.Create(
                    name,
                    description,
                    triggerType,
                    triggerConfigJson,
                    actionType,
                    actionConfigJson,
                    scopeType,
                    maxUsagePerUser,
                    maxUsagePerOrder,
                    startDate,
                    endDate,
                    isActive: isActive);
                db.MallPromotions.Add(promotion);
                promotionsByName.Add(name, promotion);
            }
            else
            {
                promotion.Update(
                    name,
                    description,
                    triggerType,
                    triggerConfigJson,
                    actionType,
                    actionConfigJson,
                    scopeType,
                    maxUsagePerUser,
                    maxUsagePerOrder,
                    startDate,
                    endDate);
                promotion.ChangeStatus(isActive);
                promotion.IsDeleted = false;
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
        var existingScopesByKey = existingScopes.ToDictionary(
            scope => (scope.PromotionId, scope.ScopeTargetType, scope.ScopeTargetId));
        foreach (var scope in existingScopes)
        {
            scope.IsDeleted = true;
        }

        using var scopesDocument = JsonDocument.Parse(File.ReadAllText(path));
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
                    existingScope.IsDeleted = false;
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

    private static (DateTime? StartDate, DateTime? EndDate) ResolveTimeWindow(
        JsonElement item,
        DateTime now)
    {
        var timeState = item.TryGetProperty("timeState", out var timeElement)
            ? timeElement.GetString()
            : null;
        return timeState switch
        {
            "Upcoming" => (now.AddDays(1), now.AddDays(2)),
            "Expired" => (now.AddDays(-2), now.AddDays(-1)),
            _ => (null, null)
        };
    }

    private static async Task<Dictionary<string, Guid>> SyncActivitiesAsync(
        AppDbContext db,
        string path,
        IReadOnlyDictionary<string, Guid> promotionIdsByMockId,
        CancellationToken ct)
    {
        var existingActivities = await db.MallActivities
            .IgnoreQueryFilters()
            .Where(activity => activity.Name.StartsWith("[测试]Banner链路-"))
            .ToListAsync(ct);
        var activitiesByName = existingActivities.ToDictionary(activity => activity.Name, StringComparer.Ordinal);
        var activityIdsByMockId = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var sourceActivityNames = new HashSet<string>(StringComparer.Ordinal);
        var now = DateTime.UtcNow;
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        foreach (var item in document.RootElement.GetProperty("activities").EnumerateArray())
        {
            var mockId = item.GetProperty("id").GetString()
                ?? throw new InvalidDataException("测试活动缺少 id");
            var name = item.GetProperty("name").GetString()
                ?? throw new InvalidDataException($"测试活动 {mockId} 缺少 name");
            var subtitle = item.TryGetProperty("subtitle", out var subtitleElement)
                ? subtitleElement.GetString() ?? string.Empty
                : string.Empty;
            var coverImage = item.TryGetProperty("coverImage", out var coverElement)
                ? coverElement.GetString() ?? string.Empty
                : string.Empty;
            var description = item.TryGetProperty("description", out var descriptionElement)
                ? descriptionElement.GetString() ?? string.Empty
                : string.Empty;
            var detailHtml = item.TryGetProperty("detailHtml", out var detailElement)
                ? detailElement.GetString() ?? string.Empty
                : string.Empty;
            var sortOrder = item.TryGetProperty("sortOrder", out var sortElement)
                ? sortElement.GetInt32()
                : 0;
            var isActive = !item.TryGetProperty("isActive", out var activeElement) || activeElement.GetBoolean();
            var (startDate, endDate) = ResolveTimeWindow(item, now);
            sourceActivityNames.Add(name);
            if (!activitiesByName.TryGetValue(name, out var activity))
            {
                activity = MallActivity.Create(
                    name,
                    subtitle,
                    coverImage,
                    description,
                    detailHtml,
                    startDate,
                    endDate,
                    sortOrder: sortOrder,
                    isActive);
                db.MallActivities.Add(activity);
                activitiesByName.Add(name, activity);
            }
            else
            {
                activity.Update(name, subtitle, coverImage, description, detailHtml, startDate, endDate, sortOrder);
                activity.ChangeStatus(isActive);
                activity.IsDeleted = false;
            }

            activityIdsByMockId.Add(mockId, activity.Id);
        }

        foreach (var activity in existingActivities.Where(activity => !sourceActivityNames.Contains(activity.Name)))
        {
            activity.ChangeStatus(false);
        }

        await db.SaveChangesAsync(ct);

        var activityIds = activityIdsByMockId.Values.ToArray();
        var existingLinks = await db.MallActivityPromotions
            .IgnoreQueryFilters()
            .Where(link => activityIds.Contains(link.ActivityId))
            .ToListAsync(ct);
        var existingLinksByPair = existingLinks.ToDictionary(
            link => (link.ActivityId, link.PromotionId));
        foreach (var link in existingLinks)
        {
            link.IsDeleted = true;
        }
        using var linkDocument = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var item in linkDocument.RootElement.GetProperty("activities").EnumerateArray())
        {
            var mockId = item.GetProperty("id").GetString()!;
            var activityId = activityIdsByMockId[mockId];
            if (!item.TryGetProperty("promotionIds", out var promotionElements))
            {
                continue;
            }

            foreach (var promotionMockId in promotionElements.EnumerateArray().Select(element => element.GetString()!).Distinct())
            {
                if (!promotionIdsByMockId.TryGetValue(promotionMockId, out var promotionId))
                {
                    throw new InvalidDataException($"测试活动 {mockId} 引用了不存在的促销: {promotionMockId}");
                }

                if (existingLinksByPair.TryGetValue((activityId, promotionId), out var existingLink))
                {
                    existingLink.IsDeleted = false;
                }
                else
                {
                    var link = MallActivityPromotion.Create(activityId, promotionId);
                    db.MallActivityPromotions.Add(link);
                    existingLinksByPair.Add((activityId, promotionId), link);
                }
            }
        }

        await db.SaveChangesAsync(ct);
        return activityIdsByMockId;
    }

    private static List<MallBanner> ParseBanners(
        string path,
        IReadOnlyDictionary<string, Guid> activityIdsByMockId,
        IReadOnlyDictionary<string, Guid> productIdsByMockId,
        IReadOnlyDictionary<string, Guid> categoryIdsByMockId)
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
            var linkType = MallBannerLinkType.ExternalUrl;
            if (item.TryGetProperty("linkType", out var linkTypeElement))
            {
                var linkTypeName = linkTypeElement.GetString();
                if (!Enum.TryParse(linkTypeName, ignoreCase: true, out linkType))
                {
                    throw new InvalidDataException($"Banner {title} 的跳转类型无效: {linkTypeName}");
                }
            }

            if (linkType == MallBannerLinkType.InternalActivity)
            {
                var activityMockId = item.TryGetProperty("activityId", out var activityElement)
                    ? activityElement.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(activityMockId) ||
                    !activityIdsByMockId.TryGetValue(activityMockId, out var activityId))
                {
                    throw new InvalidDataException($"Banner {title} 引用了不存在的测试活动: {activityMockId}");
                }

                link = activityId.ToString();
            }
            else if (linkType == MallBannerLinkType.InternalProduct)
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
                position: "home",
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
        string path,
        Guid storeId,
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
                // 首页精选属于后台运营配置，已存在商品不按 mock 种子覆盖
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
                product.IsDeleted = false;
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
                link.IsDeleted = true;
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