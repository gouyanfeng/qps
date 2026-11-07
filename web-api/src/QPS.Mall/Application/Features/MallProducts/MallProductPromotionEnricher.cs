using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.Mall.Application.Features.MallPromotions;

namespace QPS.Mall.Application.Features.MallProducts;

public class MallProductPromotionEnricher
{
    private const string AmountAction = "DiscountAmount";
    private const string DiscountRateAction = "DiscountRate";

    private readonly IMallDbContext _dbContext;
    private readonly MallPromotionEngine _promotionEngine;

    public MallProductPromotionEnricher(IMallDbContext dbContext, MallPromotionEngine promotionEngine)
    {
        _dbContext = dbContext;
        _promotionEngine = promotionEngine;
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
        // 促销生效期与下单/购物车一致，统一使用服务器本地时间
        var now = DateTime.Now;
        var promotions = await _dbContext.MallPromotions
            .AsNoTracking()
            .WhereEffective(now)
            .OrderByDescending(promotion => promotion.CreatedAt)
            .ToListAsync(cancellationToken);

        var promotionIds = promotions.Select(p => p.Id).ToList();
        var promotionScopeMap = promotionIds.Count == 0
            ? new Dictionary<Guid, List<MallPromotionScope>>()
            : await _dbContext.MallPromotionScopes
                .Where(s => promotionIds.Contains(s.PromotionId))
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
                .Where(t => promotionTagIds.Contains(t.Id) && t.Type == MallTagType.Promotion && t.IsActive)
                .ToDictionaryAsync(t => t.Id, cancellationToken);

        foreach (var product in products)
        {
            var categoryIds = categoryIdsByProduct.GetValueOrDefault(product.Id) ?? Array.Empty<Guid>();
            var appliedAmountRecords = new List<AppliedPromotionRecord>();

            foreach (var sku in product.Skus.Where(sku => sku.IsActive))
            {
                sku.Promotions = promotions
                    .Where(promotion => MallPromotionEngine.AppliesToSku(
                        promotion,
                        promotionScopeMap.TryGetValue(promotion.Id, out var scopes) ? scopes : new List<MallPromotionScope>(),
                        product.Id,
                        sku.Id,
                        product.CategoryId,
                        categoryIds))
                    .Select(ToDto)
                    .ToList();

                // 折后价与折扣标签统一由促销引擎逐行择优产出，避免展示价与结算价出现第二套计算
                var appliedRow = ApplyToSingleSku(product, sku, categoryIds, promotions, promotionScopeMap);
                appliedAmountRecords.AddRange(
                    appliedRow.AppliedPromotions.Where(record => record.ActionType == AmountAction));

                var rateApplied = appliedRow.AppliedPromotions
                    .Where(record => record.ActionType == DiscountRateAction)
                    .OrderByDescending(record => record.DiscountAmount)
                    .FirstOrDefault();
                if (rateApplied is not null && sku.Price > 0)
                {
                    var discountedPrice = Math.Round(sku.Price - rateApplied.DiscountAmount, 2);
                    if (discountedPrice < sku.Price)
                    {
                        sku.DiscountedPrice = discountedPrice;
                        sku.DiscountLabel = $"{discountedPrice / sku.Price * 10:0.#}折";
                    }
                }
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

            // 商品图角标只针对满减促销：优先取引擎实际命中的满减记录，按抵扣金额选最划算的一条；
            // 单行未达门槛时仍展示“参加满减”角标（订单累计达标后才抵扣），避免同分类商品角标缺失。
            var badgeRecord = appliedAmountRecords
                .GroupBy(record => record.PromotionId)
                .Select(group => group.OrderByDescending(record => record.DiscountAmount).First())
                .OrderByDescending(record => record.DiscountAmount)
                .ThenByDescending(record =>
                    promotions.FirstOrDefault(promotion => promotion.Id == record.PromotionId)?.CreatedAt
                    ?? DateTime.MinValue)
                .ThenBy(record => record.PromotionId)
                .FirstOrDefault();
            var badgePromotion = badgeRecord is not null
                ? promotions.FirstOrDefault(promotion => promotion.Id == badgeRecord.PromotionId)
                : product.Promotions
                    .Where(promotion => promotion.ActionType == AmountAction)
                    .Select(promotion => promotions.FirstOrDefault(entity => entity.Id == promotion.Id))
                    .OfType<MallPromotion>()
                    .OrderByDescending(promotion => AmountFromConfig(promotion.ActionConfigJson))
                    .ThenByDescending(promotion => promotion.CreatedAt)
                    .ThenBy(promotion => promotion.Id)
                    .FirstOrDefault();
            if (badgePromotion is not null)
            {
                var badgeTag = badgePromotion.PromotionTagId.HasValue &&
                    promotionTagsById.TryGetValue(badgePromotion.PromotionTagId.Value, out var matchedTag)
                        ? matchedTag
                        : null;
                product.PromotionBadgeText = badgeTag?.Name ?? badgePromotion.Name;
                product.PromotionBadgeColor = badgeTag?.Color ?? string.Empty;
            }
        }
    }

    /// <summary>按单 SKU 数量 1 构造订单上下文，交由促销引擎计算到手价与命中明细。</summary>
    private SkuOrderRow ApplyToSingleSku(
        MallProductDto product,
        MallSkuDto sku,
        Guid[] categoryIds,
        List<MallPromotion> promotions,
        Dictionary<Guid, List<MallPromotionScope>> promotionScopeMap)
    {
        var orderContext = new OrderContext
        {
            Items = new List<SkuOrderRow>
            {
                new()
                {
                    SkuId = sku.Id,
                    ProductId = product.Id,
                    CategoryId = product.CategoryId,
                    CategoryIds = categoryIds.ToList(),
                    OriginalPrice = sku.Price,
                    CurrentPrice = sku.Price,
                    Quantity = 1,
                    WeightGram = sku.WeightGram
                }
            }
        };

        // 商品列表没有购物车上下文，折扣促销的订单累计门槛无法判定：
        // 展示时按折扣率给出到手价（ignoreRateThreshold），门槛仍由购物车试算与下单按完整订单判断。
        var matches = _promotionEngine.Match(promotions, promotionScopeMap, orderContext, ignoreRateThreshold: true);
        return _promotionEngine.Apply(orderContext.Items, matches).Items[0];
    }

    /// <summary>满减促销配置金额，用于同分类多促销时的角标择优。</summary>
    private static decimal AmountFromConfig(string actionConfigJson)
    {
        using var doc = JsonDocument.Parse(actionConfigJson);
        return doc.RootElement.TryGetProperty("amount", out var amount) ? amount.GetDecimal() : 0m;
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
                .Where(category => pendingIds.Contains(category.Id))
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