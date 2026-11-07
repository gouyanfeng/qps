using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Domain.Services;

public interface IActionStrategyRegistry
{
    IActionStrategy Get(string actionType);
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
    public Guid? UserId { get; set; }
    public List<SkuOrderRow> Items { get; set; } = new();
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
    /// <summary>本行实际命中的促销明细（可同时记录折扣、满减等多条）。</summary>
    public List<AppliedPromotionRecord> AppliedPromotions { get; set; } = new();
}

/// <summary>订单行命中的单条促销快照：记录促销来源与该行实际分摊到的优惠额。</summary>
public class AppliedPromotionRecord
{
    public Guid PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
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

    /// <summary>折后优惠（DiscountRate 类促销）合计。</summary>
    public decimal RateDiscountAmount => AppliedAmount("DiscountRate");

    /// <summary>满减优惠（DiscountAmount 类促销）合计。</summary>
    public decimal AmountDiscountAmount => AppliedAmount("DiscountAmount");

    /// <summary>促销优惠合计，与分项之和一致；买赠不产生金额。</summary>
    public decimal PromotionDiscountAmount => RateDiscountAmount + AmountDiscountAmount;

    private decimal AppliedAmount(string actionType)
        => Items
            .SelectMany(item => item.AppliedPromotions)
            .Where(record => record.ActionType == actionType)
            .Sum(record => record.DiscountAmount);
}