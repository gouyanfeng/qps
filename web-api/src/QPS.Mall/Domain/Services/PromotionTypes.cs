using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Domain.Services;

public interface ITriggerStrategyRegistry
{
    ITriggerStrategy Get(string triggerType);
}

public interface IActionStrategyRegistry
{
    IActionStrategy Get(string actionType);
}

public class TriggerStrategyRegistry : ITriggerStrategyRegistry
{
    private readonly Dictionary<string, ITriggerStrategy> _map;

    public TriggerStrategyRegistry(IEnumerable<ITriggerStrategy> strategies)
    {
        _map = strategies.ToDictionary(s => s.TriggerType, s => s);
    }

    public ITriggerStrategy Get(string triggerType)
    {
        if (_map.TryGetValue(triggerType, out var strategy))
            return strategy;

        throw new InvalidOperationException($"未注册 Trigger 策略: {triggerType}");
    }
}

public class ActionStrategyRegistry : IActionStrategyRegistry
{
    private readonly Dictionary<string, IActionStrategy> _map;

    public ActionStrategyRegistry(IEnumerable<IActionStrategy> strategies)
    {
        _map = strategies.ToDictionary(s => s.ActionType, s => s);
    }

    public IActionStrategy Get(string actionType)
    {
        if (_map.TryGetValue(actionType, out var strategy))
            return strategy;

        throw new InvalidOperationException($"未注册 Action 策略: {actionType}");
    }
}

public class OrderContext
{
    public decimal GoodsAmount { get; set; }
    public Guid? UserId { get; set; }
    public bool IsFirstOrder { get; set; }
    public int UserOrderCount { get; set; }
    public DateTime Now { get; set; } = DateTime.Now;
    public List<SkuOrderRow> Items { get; set; } = new();
    public Dictionary<Guid, int> PromotionUsageMap { get; set; } = new();
}

public class SkuOrderRow
{
    public Guid SkuId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public List<Guid> CategoryIds { get; set; } = new();
    public decimal OriginalPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public int Quantity { get; set; }
    public int WeightGram { get; set; }
    public bool IsDiscounted { get; set; }
    public Guid? AppliedPromotionId { get; set; }
}

public class MatchedPromotion
{
    public MallPromotion Promotion { get; set; } = null!;
    public List<SkuOrderRow> AffectedItems { get; set; } = new();
}

public class ApplyContext
{
    public List<SkuOrderRow> Items { get; set; } = new();
    public List<GiftSkuRow> GiftSkus { get; set; } = new();
    public bool IsFreightFree { get; set; }
}

public class GiftSkuRow
{
    public Guid SkuId { get; set; }
    public int Quantity { get; set; }
    public Guid PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
}

public class ApplyResult
{
    public List<SkuOrderRow> Items { get; set; } = new();
    public List<GiftSkuRow> GiftSkus { get; set; } = new();
    public bool IsFreightFree { get; set; }
}