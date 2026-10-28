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

public class SecondHalfPriceAction : IActionStrategy
{
    public string ActionType => "SecondHalfPrice";

    public void Execute(ApplyContext ctx, JsonElement config, MatchedPromotion matchedPromotion)
    {
        if (!config.TryGetProperty("skuId", out var skuIdElem)) return;
        var skuId = Guid.Parse(skuIdElem.GetString()!);
        var halfRate = config.TryGetProperty("halfRate", out var halfElem) ? halfElem.GetDecimal() : 0.5m;

        var item = ctx.Items.FirstOrDefault(i => i.SkuId == skuId);
        if (item == null || item.Quantity < 2) return;

        var discountedQuantity = item.Quantity / 2;
        var halfPrice = Math.Round(item.CurrentPrice * halfRate, 2);

        item.CurrentPrice = Math.Round(
            (item.CurrentPrice * (item.Quantity - discountedQuantity) + halfPrice * discountedQuantity) / item.Quantity,
            2);
        item.IsDiscounted = true;
        item.AppliedPromotionId = matchedPromotion.Promotion.Id;
    }
}

public class FlashSalePriceAction : IActionStrategy
{
    public string ActionType => "FlashSalePrice";

    public void Execute(ApplyContext ctx, JsonElement config, MatchedPromotion matchedPromotion)
    {
        if (!config.TryGetProperty("skuId", out var skuIdElem)) return;
        if (!config.TryGetProperty("price", out var priceElem)) return;

        var skuId = Guid.Parse(skuIdElem.GetString()!);
        var price = priceElem.GetDecimal();

        var item = ctx.Items.FirstOrDefault(i => i.SkuId == skuId);
        if (item == null) return;

        item.CurrentPrice = price;
        item.IsDiscounted = true;
        item.AppliedPromotionId = matchedPromotion.Promotion.Id;
    }
}