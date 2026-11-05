using System.Text.Json;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Domain.Services;

public class MallPromotionEngine
{
    private static readonly HashSet<string> DiscountActions = new() { "DiscountRate" };
    private static readonly HashSet<string> StackableActions = new() { "DiscountAmount", "FreightFree", "Gift" };

    private readonly IActionStrategyRegistry _actions;

    public MallPromotionEngine(IActionStrategyRegistry actions)
    {
        _actions = actions;
    }

    public List<MatchedPromotion> Match(
        List<MallPromotion> promotions,
        Dictionary<Guid, List<MallPromotionScope>> scopeMap,
        OrderContext ctx)
    {
        var allMatched = new List<MatchedPromotion>();

        foreach (var p in promotions)
        {
            if (!p.IsActive) continue;

            if (p.MaxUsagePerUser > 0
                && ctx.UserId.HasValue
                && ctx.PromotionUsageMap.TryGetValue(p.Id, out var used)
                && used >= p.MaxUsagePerUser)
                continue;

            var scopes = scopeMap.TryGetValue(p.Id, out var s) ? s : new List<MallPromotionScope>();
            var scopeRows = FilterByScope(p, scopes, ctx.Items);
            if (scopeRows.Count == 0) continue;

            // 所有促销统一使用订单门槛（独立列 ThresholdAmount），直接比较范围内商品金额。
            var scopedAmount = scopeRows.Sum(item => item.OriginalPrice * item.Quantity);
            if (scopedAmount < p.ThresholdAmount) continue;

            allMatched.Add(new MatchedPromotion
            {
                Promotion = p,
                AffectedItems = scopeRows
            });
        }

        var final = new List<MatchedPromotion>();

        var discountGroup = allMatched
            .Where(m => DiscountActions.Contains(m.Promotion.ActionType))
            .ToList();

        var stackableGroup = allMatched
            .Where(m => StackableActions.Contains(m.Promotion.ActionType))
            .ToList();

        final.AddRange(SelectBestDiscounts(discountGroup));
        final.AddRange(stackableGroup);

        return final;
    }

    public ApplyResult Apply(List<SkuOrderRow> skuRows, List<MatchedPromotion> matches)
    {
        var applyCtx = new ApplyContext
        {
            Items = skuRows.Select(r => new SkuOrderRow
            {
                SkuId = r.SkuId,
                ProductId = r.ProductId,
                CategoryId = r.CategoryId,
                CategoryIds = r.CategoryIds,
                OriginalPrice = r.OriginalPrice,
                CurrentPrice = r.CurrentPrice,
                Quantity = r.Quantity,
                WeightGram = r.WeightGram,
                IsDiscounted = false
            }).ToList()
        };

        foreach (var m in matches)
        {
            using var actionDoc = JsonDocument.Parse(m.Promotion.ActionConfigJson);
            var actionConfig = actionDoc.RootElement;

            var action = _actions.Get(m.Promotion.ActionType);
            var affectedSkuIds = m.AffectedItems.Select(item => item.SkuId).ToHashSet();
            var scopedItems = applyCtx.Items
                .Where(item => affectedSkuIds.Contains(item.SkuId))
                .Where(item => !item.IsDiscounted || !DiscountActions.Contains(m.Promotion.ActionType))
                .ToList();
            var scopedContext = new ApplyContext
            {
                Items = scopedItems,
                GiftSkus = applyCtx.GiftSkus,
                IsFreightFree = applyCtx.IsFreightFree
            };
            action.Execute(scopedContext, actionConfig, m);
            applyCtx.IsFreightFree = scopedContext.IsFreightFree;
        }

        return new ApplyResult
        {
            Items = applyCtx.Items,
            GiftSkus = applyCtx.GiftSkus,
            IsFreightFree = applyCtx.IsFreightFree
        };
    }

    private static List<MatchedPromotion> SelectBestDiscounts(List<MatchedPromotion> discountGroup)
    {
        if (discountGroup.Count == 0) return new List<MatchedPromotion>();
        if (discountGroup.Count == 1) return discountGroup;

        var bestBySku = new Dictionary<Guid, MatchedPromotion>();

        foreach (var m in discountGroup)
        {
            var eval = EvaluateDiscountSavings(m);
            foreach (var item in m.AffectedItems)
            {
                var skuId = item.SkuId;
                if (!bestBySku.TryGetValue(skuId, out var existing))
                {
                    bestBySku[skuId] = m;
                    continue;
                }
                var existingEval = EvaluateDiscountSavings(existing);
                if (eval > existingEval)
                    bestBySku[skuId] = m;
            }
        }

        return bestBySku.Values.DistinctBy(x => x.Promotion.Id).ToList();
    }

    private static decimal EvaluateDiscountSavings(MatchedPromotion m)
    {
        using var actionDoc = JsonDocument.Parse(m.Promotion.ActionConfigJson);
        var actionConfig = actionDoc.RootElement;

        if (!actionConfig.TryGetProperty("rate", out var rateElem)) return 0;
        var rate = rateElem.GetDecimal();
        if (rate >= 1) return 0;

        var originalAmount = m.AffectedItems.Sum(i => i.OriginalPrice * i.Quantity);
        return originalAmount * (1 - rate);
    }

    private static List<SkuOrderRow> FilterByScope(MallPromotion p, List<MallPromotionScope> scopes, List<SkuOrderRow> items)
    {
        if (p.ScopeType == "Global") return items.ToList();

        return p.ScopeType switch
        {
            "Product" => items.Where(i => scopes.Any(s => s.ScopeTargetType == "Product" && s.ScopeTargetId == i.ProductId)).ToList(),
            "Category" => items.Where(i => scopes.Any(s =>
                s.ScopeTargetType == "Category" &&
                (i.CategoryIds.Contains(s.ScopeTargetId) || i.CategoryId == s.ScopeTargetId))).ToList(),
            "Sku" => items.Where(i => scopes.Any(s => s.ScopeTargetType == "Sku" && s.ScopeTargetId == i.SkuId)).ToList(),
            _ => new List<SkuOrderRow>()
        };
    }
}