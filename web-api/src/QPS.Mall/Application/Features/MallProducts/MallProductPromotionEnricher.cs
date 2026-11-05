using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Application.Features.MallPromotions;

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
            .WhereEffective(now)
            .OrderByDescending(promotion => promotion.CreatedAt)
            .ToListAsync(cancellationToken);

        var promotionIds = promotions.Select(p => p.Id).ToList();
        var promotionScopeMap = promotionIds.Count == 0
            ? new Dictionary<Guid, List<MallPromotionScope>>()
            : await _dbContext.MallPromotionScopes
                .Where(s => promotionIds.Contains(s.PromotionId) && !s.IsDeleted)
                .GroupBy(s => s.PromotionId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken);

        // 促销标签：命中的促销所配置的标签合并进商品展示标签
        var promotionTagIds = promotions
            .Where(p => p.PromotionTagId.HasValue)
            .Select(p => p.PromotionTagId!.Value)
            .Distinct()
            .ToList();
        var promotionTagsById = promotionTagIds.Count == 0
            ? new Dictionary<Guid, MallTag>()
            : await _dbContext.MallTags
                .AsNoTracking()
                .Where(t => promotionTagIds.Contains(t.Id) && t.Type == MallTagType.Promotion && t.IsActive && !t.IsDeleted)
                .ToDictionaryAsync(t => t.Id, cancellationToken);

        foreach (var product in products)
        {
            foreach (var sku in product.Skus.Where(sku => sku.IsActive))
            {
                sku.Promotions = promotions
                    .Where(promotion => AppliesToSku(
                        promotion,
                        promotionScopeMap.TryGetValue(promotion.Id, out var scopes) ? scopes : new List<MallPromotionScope>(),
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
                .ToList();

            var matchedTagIds = product.Promotions
                .Select(p => promotions.FirstOrDefault(pe => pe.Id == p.Id))
                .Where(pe => pe?.PromotionTagId.HasValue == true)
                .Select(pe => pe!.PromotionTagId!.Value)
                .Distinct();
            foreach (var tagId in matchedTagIds)
            {
                if (promotionTagsById.TryGetValue(tagId, out var tag) &&
                    product.Tags.All(t => t.Id != tag.Id))
                {
                    product.Tags.Add(new MallTagDto
                    {
                        Id = tag.Id,
                        Type = tag.Type,
                        Code = tag.Code,
                        Name = tag.Name,
                        Color = tag.Color,
                        SortOrder = tag.SortOrder,
                        IsActive = tag.IsActive
                    });
                }
            }
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
        List<MallPromotionScope> scopes,
        Guid productId,
        Guid skuId,
        Guid? categoryId,
        IReadOnlyCollection<Guid> categoryAncestors)
    {
        return promotion.ScopeType switch
        {
            "Global" => true,
            "Product" => scopes.Any(scope =>
                scope.ScopeTargetType == "Product" && scope.ScopeTargetId == productId),
            "Sku" => scopes.Any(scope =>
                scope.ScopeTargetType == "Sku" && scope.ScopeTargetId == skuId),
            "Category" => scopes.Any(scope =>
                scope.ScopeTargetType == "Category" &&
                (scope.ScopeTargetId == categoryId || categoryAncestors.Contains(scope.ScopeTargetId))),
            _ => false
        };
    }

    private static MallProductPromotionDto ToDto(MallPromotion promotion)
    {
        return new MallProductPromotionDto
        {
            Id = promotion.Id,
            Name = promotion.Name,
            Description = promotion.Description,
            PromotionType = promotion.PromotionType,
            ThresholdAmount = promotion.ThresholdAmount,
            ActionType = promotion.ActionType,
            ActionConfigJson = promotion.ActionConfigJson,
            ScopeType = promotion.ScopeType,
            RuleText = BuildRuleText(promotion),
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate
        };
    }

    private static string BuildRuleText(MallPromotion promotion)
    {
        using var actionConfig = JsonDocument.Parse(promotion.ActionConfigJson);
        var action = actionConfig.RootElement;
        var condition = promotion.ThresholdAmount > 0.01m
            ? $"满 ¥{promotion.ThresholdAmount:0.##}"
            : string.Empty;

        var benefit = promotion.ActionType switch
        {
            "DiscountAmount" when action.TryGetProperty("amount", out var discountAmount)
                => $"减 ¥{discountAmount.GetDecimal():0.##}",
            "DiscountRate" when action.TryGetProperty("rate", out var rate)
                => $"{rate.GetDecimal() * 10:0.#} 折",
            "Gift" when action.TryGetProperty("quantity", out var giftQuantity)
                => $"赠 {giftQuantity.GetInt32()} 件",
            "Gift" => "赠送商品",
            _ => promotion.Description
        };

        return string.IsNullOrEmpty(condition) ? benefit : $"{condition}，{benefit}";
    }
}