using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallProducts;

public class MallProductPromotionEnricher
{
    private readonly IMallDbContext _dbContext;

    public MallProductPromotionEnricher(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnrichAsync(
        IReadOnlyCollection<MallProductDto> products,
        CancellationToken cancellationToken)
    {
        if (products.Count == 0)
        {
            return;
        }

        var categoryIdsByProduct = await LoadCategoryAncestorsAsync(products, cancellationToken);
        var now = DateTime.UtcNow;
        var promotions = await _dbContext.MallPromotions
            .AsNoTracking()
            .Include(promotion => promotion.Scopes.Where(scope => !scope.IsDeleted))
            .Where(promotion =>
                promotion.IsActive &&
                !promotion.IsDeleted &&
                (promotion.StartDate == null || promotion.StartDate <= now) &&
                (promotion.EndDate == null || promotion.EndDate >= now))
            .OrderByDescending(promotion => promotion.Priority)
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            foreach (var sku in product.Skus.Where(sku => sku.IsActive))
            {
                sku.Promotions = promotions
                    .Where(promotion => AppliesToSku(
                        promotion,
                        product.Id,
                        sku.Id,
                        product.CategoryId,
                        categoryIdsByProduct.GetValueOrDefault(product.Id) ?? Array.Empty<Guid>()))
                    .Select(ToDto)
                    .ToList();
            }

            product.Promotions = product.Skus
                .SelectMany(sku => sku.Promotions)
                .DistinctBy(promotion => promotion.Id)
                .OrderByDescending(promotion => promotion.Priority)
                .ToList();
        }
    }

    private async Task<Dictionary<Guid, Guid[]>> LoadCategoryAncestorsAsync(
        IReadOnlyCollection<MallProductDto> products,
        CancellationToken cancellationToken)
    {
        var pendingIds = products
            .Where(product => product.CategoryId.HasValue)
            .Select(product => product.CategoryId!.Value)
            .Distinct()
            .ToHashSet();
        var parentById = new Dictionary<Guid, Guid?>();

        while (pendingIds.Count > 0)
        {
            var categories = await _dbContext.MallCategories
                .AsNoTracking()
                .Where(category => pendingIds.Contains(category.Id) && !category.IsDeleted)
                .Select(category => new { category.Id, category.ParentId })
                .ToListAsync(cancellationToken);
            pendingIds.Clear();

            foreach (var category in categories)
            {
                parentById[category.Id] = category.ParentId;
                if (category.ParentId.HasValue && !parentById.ContainsKey(category.ParentId.Value))
                {
                    pendingIds.Add(category.ParentId.Value);
                }
            }
        }

        return products.ToDictionary(
            product => product.Id,
            product =>
            {
                var ancestors = new List<Guid>();
                var currentId = product.CategoryId;
                var visited = new HashSet<Guid>();
                while (currentId.HasValue && visited.Add(currentId.Value))
                {
                    ancestors.Add(currentId.Value);
                    currentId = parentById.GetValueOrDefault(currentId.Value);
                }

                return ancestors.ToArray();
            });
    }

    private static bool AppliesToSku(
        MallPromotion promotion,
        Guid productId,
        Guid skuId,
        Guid? categoryId,
        IReadOnlyCollection<Guid> categoryAncestors)
    {
        var inScope = promotion.ScopeType switch
        {
            "Global" => true,
            "Product" => promotion.Scopes.Any(scope =>
                scope.ScopeTargetType == "Product" && scope.ScopeTargetId == productId),
            "Sku" => promotion.Scopes.Any(scope =>
                scope.ScopeTargetType == "Sku" && scope.ScopeTargetId == skuId),
            "Category" => promotion.Scopes.Any(scope =>
                scope.ScopeTargetType == "Category" &&
                (scope.ScopeTargetId == categoryId || categoryAncestors.Contains(scope.ScopeTargetId))),
            _ => false
        };
        if (!inScope)
        {
            return false;
        }

        if (promotion.TriggerType != "BuyProduct")
        {
            return true;
        }

        using var triggerConfig = JsonDocument.Parse(promotion.TriggerConfigJson);
        if (!triggerConfig.RootElement.TryGetProperty("productIds", out var productIds))
        {
            return true;
        }

        return productIds.EnumerateArray()
            .Any(item => Guid.TryParse(item.GetString(), out var triggerProductId) && triggerProductId == productId);
    }

    private static MallProductPromotionDto ToDto(MallPromotion promotion)
    {
        return new MallProductPromotionDto
        {
            Id = promotion.Id,
            Name = promotion.Name,
            Description = promotion.Description,
            TriggerType = promotion.TriggerType,
            TriggerConfigJson = promotion.TriggerConfigJson,
            ActionType = promotion.ActionType,
            ActionConfigJson = promotion.ActionConfigJson,
            ScopeType = promotion.ScopeType,
            RuleText = BuildRuleText(promotion),
            Priority = promotion.Priority,
            Combinable = promotion.Combinable,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate
        };
    }

    private static string BuildRuleText(MallPromotion promotion)
    {
        using var triggerConfig = JsonDocument.Parse(promotion.TriggerConfigJson);
        using var actionConfig = JsonDocument.Parse(promotion.ActionConfigJson);
        var trigger = triggerConfig.RootElement;
        var action = actionConfig.RootElement;
        var condition = promotion.TriggerType switch
        {
            "FullAmount" when trigger.TryGetProperty("minAmount", out var amount) && amount.GetDecimal() > 0.01m
                => $"满 ¥{amount.GetDecimal():0.##}",
            "BuyProduct" when trigger.TryGetProperty("minQuantity", out var quantity)
                => $"买满 {quantity.GetInt32()} 件",
            "BuyProduct" => "购买指定商品",
            "FirstOrder" => "首单",
            "OrderCount" when trigger.TryGetProperty("orderCount", out var orderCount)
                => $"累计下单不超过 {orderCount.GetInt32()} 次",
            "FlashSale" => "限时",
            _ => string.Empty
        };

        var benefit = promotion.ActionType switch
        {
            "DiscountAmount" when action.TryGetProperty("amount", out var discountAmount)
                => $"减 ¥{discountAmount.GetDecimal():0.##}",
            "DiscountRate" when action.TryGetProperty("rate", out var rate)
                => $"{rate.GetDecimal() * 10:0.#} 折",
            "Gift" when action.TryGetProperty("quantity", out var giftQuantity)
                => $"赠 {giftQuantity.GetInt32()} 件",
            "Gift" => "赠送商品",
            "FreightFree" => "免运费",
            "SecondHalfPrice" => "第二件半价",
            "FlashSalePrice" when action.TryGetProperty("price", out var flashPrice)
                => $"特价 ¥{flashPrice.GetDecimal():0.##}",
            _ => promotion.Description
        };

        return string.IsNullOrEmpty(condition) ? benefit : $"{condition}，{benefit}";
    }
}
