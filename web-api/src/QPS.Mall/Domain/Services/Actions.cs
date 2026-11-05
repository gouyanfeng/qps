using System.Text.Json;

namespace QPS.Mall.Domain.Services;

public class GiftAction : IActionStrategy
{
    public string ActionType => "Gift";

    public void Execute(ApplyContext ctx, JsonElement config, MatchedPromotion matchedPromotion)
    {
        if (!config.TryGetProperty("skuId", out var skuIdElem)) return;
        var skuId = Guid.Parse(skuIdElem.GetString()!);
        var quantity = config.TryGetProperty("quantity", out var qtyElem) ? qtyElem.GetInt32() : 1;

        ctx.GiftSkus.Add(new GiftSkuRow
        {
            SkuId = skuId,
            Quantity = quantity,
            PromotionId = matchedPromotion.Promotion.Id,
            PromotionName = matchedPromotion.Promotion.Name
        });
    }
}

public class DiscountAmountAction : IActionStrategy
{
    public string ActionType => "DiscountAmount";

    public void Execute(ApplyContext ctx, JsonElement config, MatchedPromotion matchedPromotion)
    {
        if (!config.TryGetProperty("amount", out var amountElem)) return;
        var discount = amountElem.GetDecimal();

        var total = ctx.Items.Sum(i => i.CurrentPrice * i.Quantity);
        if (total <= 0) return;

        foreach (var item in ctx.Items)
        {
            var proportion = item.CurrentPrice * item.Quantity / total;
            var itemDiscount = discount * proportion;
            var newPrice = Math.Max(0, item.CurrentPrice - itemDiscount / item.Quantity);
            item.CurrentPrice = Math.Round(newPrice, 2);
            item.IsDiscounted = true;
            item.AppliedPromotionId = matchedPromotion.Promotion.Id;
        }
    }
}

public class DiscountRateAction : IActionStrategy
{
    public string ActionType => "DiscountRate";

    public void Execute(ApplyContext ctx, JsonElement config, MatchedPromotion matchedPromotion)
    {
        if (!config.TryGetProperty("rate", out var rateElem)) return;
        var rate = rateElem.GetDecimal();

        foreach (var item in ctx.Items)
        {
            item.CurrentPrice = Math.Round(item.CurrentPrice * rate, 2);
            item.IsDiscounted = true;
            item.AppliedPromotionId = matchedPromotion.Promotion.Id;
        }
    }
}

public class FreightFreeAction : IActionStrategy
{
    public string ActionType => "FreightFree";

    public void Execute(ApplyContext ctx, JsonElement config, MatchedPromotion matchedPromotion)
    {
        ctx.IsFreightFree = true;
    }
}