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

public class FirstOrderTrigger : ITriggerStrategy
{
    public string TriggerType => "FirstOrder";

    public bool IsMatch(OrderContext ctx, JsonElement config)
    {
        return ctx.IsFirstOrder;
    }
}

public class OrderCountTrigger : ITriggerStrategy
{
    public string TriggerType => "OrderCount";

    public bool IsMatch(OrderContext ctx, JsonElement config)
    {
        if (!config.TryGetProperty("orderCount", out var orderCount)) return false;
        return ctx.UserOrderCount <= orderCount.GetInt32();
    }
}

public class FlashSaleTrigger : ITriggerStrategy
{
    public string TriggerType => "FlashSale";

    public bool IsMatch(OrderContext ctx, JsonElement config)
    {
        if (config.TryGetProperty("startTime", out var startElem) && config.TryGetProperty("endTime", out var endElem))
        {
            var start = DateTime.Parse(startElem.GetString()!);
            var end = DateTime.Parse(endElem.GetString()!);
            return ctx.Now >= start && ctx.Now <= end;
        }
        return true;
    }
}