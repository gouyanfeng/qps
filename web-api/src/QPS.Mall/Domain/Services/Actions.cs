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

        var remaining = Math.Min(discount, total);
        var remainingAmount = total;

        for (var index = 0; index < ctx.Items.Count; index++)
        {
            var item = ctx.Items[index];
            var itemAmount = item.CurrentPrice * item.Quantity;
            if (itemAmount <= 0) continue;
            if (remaining <= 0) break;

            // 前 N-1 行按金额占比四舍五入到分；最后一行吃掉全部尾差，保证 Σ行优惠 == 订单总优惠。
            var share = index == ctx.Items.Count - 1
                ? remaining
                : Math.Round(remaining * itemAmount / remainingAmount, 2);
            share = Math.Max(0, Math.Min(share, itemAmount));

            var newPrice = Math.Max(0, Math.Round((itemAmount - share) / item.Quantity, 2));
            item.CurrentPrice = newPrice;
            item.IsDiscounted = true;

            // 按最终单价回算本行实际优惠，尾差在后续行（最后一行）补齐。
            remaining = Math.Max(0, remaining - (itemAmount - newPrice * item.Quantity));
            remainingAmount -= itemAmount;
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