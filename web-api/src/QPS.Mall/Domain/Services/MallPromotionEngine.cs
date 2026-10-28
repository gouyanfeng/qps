using System.Text.Json;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Domain.Services;

public class MallPromotionEngine
{
    private readonly ITriggerStrategyRegistry _triggers;
    private readonly IActionStrategyRegistry _actions;

    public MallPromotionEngine(
        ITriggerStrategyRegistry triggers,
        IActionStrategyRegistry actions)
    {
        _triggers = triggers;
        _actions = actions;
    }

    public List<MatchedPromotion> Match(
        List<MallPromotion> promotions,
        OrderContext ctx)
    {
        var now = ctx.Now;
        var result = new List<MatchedPromotion>();

        foreach (var p in promotions)
        {
            if (!p.IsActive || p.IsDeleted) continue;
            if (!p.IsWithinTimeWindow(now)) continue;

            if (p.MaxUsagePerUser > 0
                && ctx.UserId.HasValue
                && ctx.PromotionUsageMap.TryGetValue(p.Id, out var used)
                && used >= p.MaxUsagePerUser)
                continue;

            var scopeRows = FilterByScope(p, ctx.Items);
            if (scopeRows.Count == 0) continue;

            var scopedTriggerContext = new OrderContext
            {
                GoodsAmount = scopeRows.Sum(item => item.OriginalPrice * item.Quantity),
                UserId = ctx.UserId,
                IsFirstOrder = ctx.IsFirstOrder,
                UserOrderCount = ctx.UserOrderCount,
                Now = ctx.Now,
                Items = scopeRows,
                PromotionUsageMap = ctx.PromotionUsageMap
            };
            var triggerContext = p.TriggerType == "BuyProduct" ? ctx : scopedTriggerContext;

            using var triggerDoc = JsonDocument.Parse(p.TriggerConfigJson);
            var triggerConfig = triggerDoc.RootElement;

            var strategy = _triggers.Get(p.TriggerType);
            if (!strategy.IsMatch(triggerContext, triggerConfig)) continue;

            result.Add(new MatchedPromotion
            {
                Promotion = p,
                AffectedItems = scopeRows
            });
        }

        result = result
            .OrderByDescending(m => m.Promotion.Priority)
            .ToList();

        var final = new List<MatchedPromotion>();
        var combinableCount = 0;

        foreach (var m in result)
        {
            if (!m.Promotion.Combinable && final.Count > 0) continue;

            var unprotected = m.AffectedItems.Where(i => !i.IsDiscounted).ToList();
            if (unprotected.Count == 0 && m.Promotion.ActionType != "FreightFree" && m.Promotion.ActionType != "Gift")
                continue;

            final.Add(new MatchedPromotion
            {
                Promotion = m.Promotion,
                AffectedItems = unprotected.Count > 0 ? unprotected : m.AffectedItems
            });

            combinableCount++;
        }

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
                .Where(item => !item.IsDiscounted || m.Promotion.ActionType is "Gift" or "FreightFree")
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

    private static List<SkuOrderRow> FilterByScope(MallPromotion p, List<SkuOrderRow> items)
    {
        if (p.ScopeType == "Global") return items.ToList();

        var scope = p.Scopes.ToList();

        return p.ScopeType switch
        {
            "Product" => items.Where(i => scope.Any(s => s.ScopeTargetType == "Product" && s.ScopeTargetId == i.ProductId)).ToList(),
            "Category" => items.Where(i => scope.Any(s =>
                s.ScopeTargetType == "Category" &&
                (i.CategoryIds.Contains(s.ScopeTargetId) || i.CategoryId == s.ScopeTargetId))).ToList(),
            "Sku" => items.Where(i => scope.Any(s => s.ScopeTargetType == "Sku" && s.ScopeTargetId == i.SkuId)).ToList(),
            _ => new List<SkuOrderRow>()
        };
    }
}