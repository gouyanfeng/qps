using System.Text.Json;

namespace QPS.Mall.Domain.Services;

public class FullAmountTrigger : ITriggerStrategy
{
    public string TriggerType => "FullAmount";

    public bool IsMatch(OrderContext ctx, JsonElement config)
    {
        if (!config.TryGetProperty("minAmount", out var minAmount)) return false;
        return ctx.GoodsAmount >= minAmount.GetDecimal();
    }
}

public class BuyProductTrigger : ITriggerStrategy
{
    public string TriggerType => "BuyProduct";

    public bool IsMatch(OrderContext ctx, JsonElement config)
    {
        var minQuantity = config.TryGetProperty("minQuantity", out var quantity)
            ? quantity.GetInt32()
            : 1;
        if (minQuantity < 1) return false;

        if (!config.TryGetProperty("productIds", out var ids))
        {
            return ctx.Items.Sum(item => item.Quantity) >= minQuantity;
        }

        var idList = ids.EnumerateArray().Select(x => Guid.Parse(x.GetString()!)).ToList();
        return idList.Any(productId =>
            ctx.Items.Where(item => item.ProductId == productId).Sum(item => item.Quantity) >= minQuantity);
    }
}