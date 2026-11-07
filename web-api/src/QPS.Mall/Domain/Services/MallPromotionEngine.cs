using System.Text.Json;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Domain.Services;

/// <summary>
/// 促销引擎（规则 v2）。
/// 层级由动作类型决定，作用范围只决定覆盖范围：
/// - DiscountAmount（满减）：订单级，按覆盖范围的行级折扣后小计判断门槛；覆盖商品有交集的两条互斥（取抵扣最大），完全不重叠则各自生效；
/// - DiscountRate（折扣）：行级，逐行择优，同一行只生效一条（优惠额最大；同额按作用范围具体度 SKU &gt; 商品 &gt; 分类 &gt; 全场，再按 CreatedAt/Id）；
/// - Gift（买赠）：行级，0 元计价，不参与比价，直接生效，可与折扣并存。
/// 计算顺序固定：商品原价 → 行级折扣（逐行择优）→ 买赠（0 元，不改金额）→ 整单满减（门槛按范围内行级之后的金额）→ 券 → 应付 = max(0, …)。
/// 满减与折扣/赠品可叠加；有交集的满减之间、同一行的折扣之间互斥。
/// 促销不做使用次数限制。
/// </summary>
public class MallPromotionEngine
{
    private const string ActionDiscountAmount = "DiscountAmount";
    private const string ActionDiscountRate = "DiscountRate";
    private const string ActionGift = "Gift";
    private const string ActionFreightFree = "FreightFree";

    private readonly IActionStrategyRegistry _actions;

    public MallPromotionEngine(IActionStrategyRegistry actions)
    {
        _actions = actions;
    }

    /// <summary>
    /// 命中促销：只做范围与行级门槛过滤，不做满减门槛判断（满减门槛依赖行级折扣后的金额，在 Apply 阶段计算）。
    /// 返回顺序固定为 CreatedAt → Id，保证择优结果可复现。
    /// </summary>
    /// <param name="ignoreRateThreshold">
    /// 仅商品列表展示使用：列表没有购物车上下文，折扣促销的订单累计门槛无法判定，
    /// 展示时按折扣率给出到手价，门槛仍由购物车试算与下单按完整订单判断。
    /// 结算/预览调用不传该参数，保持原有门槛语义。
    /// </param>
    public List<MatchedPromotion> Match(
        List<MallPromotion> promotions,
        Dictionary<Guid, List<MallPromotionScope>> scopeMap,
        OrderContext ctx,
        bool ignoreRateThreshold = false)
    {
        var allMatched = new List<MatchedPromotion>();

        foreach (var p in promotions
                     .Where(p => p.IsActive)
                     .OrderBy(p => p.CreatedAt)
                     .ThenBy(p => p.Id))
        {
            var scopes = scopeMap.TryGetValue(p.Id, out var s) ? s : new List<MallPromotionScope>();
            var scopeRows = FilterByScope(p, scopes, ctx.Items);
            if (scopeRows.Count == 0) continue;

            // 行级促销（折扣/买赠/免运费）的门槛按自身覆盖范围内的商品原价合计判断；满减留到 Apply 阶段。
            if (p.ActionType != ActionDiscountAmount)
            {
                var thresholdAmount = ignoreRateThreshold && p.ActionType == ActionDiscountRate
                    ? 0m
                    : p.ThresholdAmount;
                var scopedAmount = scopeRows.Sum(item => item.OriginalPrice * item.Quantity);
                if (scopedAmount < thresholdAmount) continue;
            }

            allMatched.Add(new MatchedPromotion
            {
                Promotion = p,
                AffectedItems = scopeRows
            });
        }

        return allMatched;
    }

    /// <summary>
    /// 按固定顺序应用促销：行级折扣（逐行择优）→ 买赠（0 元）→ 整单满减（范围内折后小计达门槛，只生效一条）。
    /// 每行实际分摊到的优惠写入 <see cref="SkuOrderRow.AppliedPromotions"/>，可还原一行命中的多条促销。
    /// </summary>
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

        var rateMatches = matches.Where(m => m.Promotion.ActionType == ActionDiscountRate).ToList();
        var giftMatches = matches.Where(m => m.Promotion.ActionType == ActionGift).ToList();
        var amountMatches = matches.Where(m => m.Promotion.ActionType == ActionDiscountAmount).ToList();
        var freightFreeMatches = matches.Where(m => m.Promotion.ActionType == ActionFreightFree).ToList();

        // 1) 行级折扣：逐行只取一条，优惠额最大者胜出；同额按作用范围具体度、CreatedAt、Id 稳定排序。
        foreach (var row in applyCtx.Items)
        {
            var candidates = rateMatches
                .Where(m => m.AffectedItems.Any(item => item.SkuId == row.SkuId))
                .ToList();
            if (candidates.Count == 0) continue;

            var best = candidates
                .OrderByDescending(m => RateSavings(m.Promotion, row))
                .ThenByDescending(m => ScopeSpecificity(m.Promotion.ScopeType))
                .ThenBy(m => m.Promotion.CreatedAt)
                .ThenBy(m => m.Promotion.Id)
                .First();

            var before = row.CurrentPrice;
            Execute(best, new ApplyContext { Items = new List<SkuOrderRow> { row } }, applyCtx);
            var discount = Math.Max(0, (before - row.CurrentPrice) * row.Quantity);
            if (discount > 0)
            {
                row.AppliedPromotions.Add(BuildRecord(best, discount));
            }
        }

        // 2) 买赠：直接生效，不参与比价，不改金额。
        foreach (var gift in giftMatches)
        {
            Execute(gift, new ApplyContext { Items = applyCtx.Items, GiftSkus = applyCtx.GiftSkus }, applyCtx);
        }

        // 3) 整单满减：门槛按各自覆盖范围内行级折扣后的金额判断。
        //    覆盖商品有交集的两条满减互斥（只取实际抵扣最大的一条）；完全不重叠则各自生效。
        var eligibleAmounts = amountMatches
            .Select(m => new
            {
                Match = m,
                ScopedAmount = ScopedCurrentAmount(m, applyCtx.Items)
            })
            .Where(x => x.ScopedAmount > 0 && x.ScopedAmount >= x.Match.Promotion.ThresholdAmount)
            .Select(x => new
            {
                x.Match,
                Amount = Math.Min(AmountValue(x.Match.Promotion), x.ScopedAmount)
            })
            .Where(x => x.Amount > 0)
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.Match.Promotion.CreatedAt)
            .ThenBy(x => x.Match.Promotion.Id)
            .ToList();

        var selectedAmounts = new List<MatchedPromotion>();
        foreach (var candidate in eligibleAmounts)
        {
            var candidateSkus = candidate.Match.AffectedItems.Select(item => item.SkuId).ToHashSet();
            var overlaps = selectedAmounts.Any(selected =>
                selected.AffectedItems.Any(item => candidateSkus.Contains(item.SkuId)));
            if (overlaps) continue;

            selectedAmounts.Add(candidate.Match);
        }

        foreach (var selected in selectedAmounts)
        {
            var scopedRows = applyCtx.Items
                .Where(row => selected.AffectedItems.Any(item => item.SkuId == row.SkuId))
                .ToList();
            var beforeBySku = scopedRows.ToDictionary(
                row => row.SkuId,
                row => row.CurrentPrice * row.Quantity);

            Execute(selected, new ApplyContext { Items = scopedRows }, applyCtx);

            foreach (var row in scopedRows)
            {
                var discount = beforeBySku[row.SkuId] - row.CurrentPrice * row.Quantity;
                if (discount > 0)
                {
                    row.AppliedPromotions.Add(BuildRecord(selected, discount));
                }
            }
        }

        // 4) 免运费：直接生效。
        foreach (var freightFree in freightFreeMatches)
        {
            Execute(freightFree, new ApplyContext { Items = applyCtx.Items }, applyCtx);
        }

        return new ApplyResult
        {
            Items = applyCtx.Items,
            GiftSkus = applyCtx.GiftSkus,
            IsFreightFree = applyCtx.IsFreightFree
        };
    }

    private void Execute(MatchedPromotion matched, ApplyContext scopedContext, ApplyContext target)
    {
        using var actionDoc = JsonDocument.Parse(matched.Promotion.ActionConfigJson);
        var action = _actions.Get(matched.Promotion.ActionType);
        action.Execute(scopedContext, actionDoc.RootElement, matched);

        if (scopedContext.IsFreightFree)
        {
            target.IsFreightFree = true;
        }
    }

    private static AppliedPromotionRecord BuildRecord(MatchedPromotion matched, decimal discount)
        => new()
        {
            PromotionId = matched.Promotion.Id,
            PromotionName = matched.Promotion.Name,
            ActionType = matched.Promotion.ActionType,
            DiscountAmount = Math.Round(discount, 2)
        };

    /// <summary>行级折扣在本行的优惠额：单价 × 折扣差额 × 数量。</summary>
    private static decimal RateSavings(MallPromotion promotion, SkuOrderRow row)
    {
        var rate = RateValue(promotion);
        if (rate >= 1) return 0;
        return Math.Max(0, row.OriginalPrice * (1 - rate) * row.Quantity);
    }

    private static decimal ScopedCurrentAmount(MatchedPromotion matched, List<SkuOrderRow> items)
    {
        var affectedSkuIds = matched.AffectedItems.Select(item => item.SkuId).ToHashSet();
        return items
            .Where(item => affectedSkuIds.Contains(item.SkuId))
            .Sum(item => item.CurrentPrice * item.Quantity);
    }

    /// <summary>作用范围具体度：SKU &gt; 商品 &gt; 分类 &gt; 全场。</summary>
    private static int ScopeSpecificity(string scopeType) => scopeType switch
    {
        "Sku" => 3,
        "Product" => 2,
        "Category" => 1,
        _ => 0
    };

    private static decimal AmountValue(MallPromotion promotion)
    {
        using var doc = JsonDocument.Parse(promotion.ActionConfigJson);
        return doc.RootElement.TryGetProperty("amount", out var amount) ? amount.GetDecimal() : 0;
    }

    private static decimal RateValue(MallPromotion promotion)
    {
        using var doc = JsonDocument.Parse(promotion.ActionConfigJson);
        return doc.RootElement.TryGetProperty("rate", out var rate) ? rate.GetDecimal() : 1;
    }

    private static List<SkuOrderRow> FilterByScope(MallPromotion p, List<MallPromotionScope> scopes, List<SkuOrderRow> items)
    {
        if (p.ScopeType == "Global") return items.ToList();

        return items
            .Where(item => AppliesToSku(
                p,
                scopes,
                item.ProductId,
                item.SkuId,
                item.CategoryId,
                item.CategoryIds))
            .ToList();
    }

    /// <summary>
    /// 统一的促销适用范围判定：商品页展示、购物车试算与下单命中均走此方法，避免多套口径。
    /// 分类范围需传入包含自身在内的完整祖先链（categoryAncestors）。
    /// </summary>
    public static bool AppliesToSku(
        MallPromotion promotion,
        IReadOnlyCollection<MallPromotionScope> scopes,
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
}
