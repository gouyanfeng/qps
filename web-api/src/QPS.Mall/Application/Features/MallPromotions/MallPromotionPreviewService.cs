using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallPromotions;

/// <summary>按商品与数量试算促销优惠，供购物车和订单确认页复用；口径与下单的促销引擎一致。</summary>
public class MallPromotionPreviewService
{
    private readonly IMallDbContext _dbContext;
    private readonly MallPromotionEngine _promotionEngine;
    private readonly MallPromotionContextBuilder _contextBuilder;

    public MallPromotionPreviewService(
        IMallDbContext dbContext,
        MallPromotionEngine promotionEngine,
        MallPromotionContextBuilder contextBuilder)
    {
        _dbContext = dbContext;
        _promotionEngine = promotionEngine;
        _contextBuilder = contextBuilder;
    }

    /// <summary>试算指定商品数量下的商品金额、促销优惠总额及逐条优惠明细。</summary>
    public async Task<MallPromotionPreview> PreviewAsync(
        Guid memberId,
        IReadOnlyCollection<MallPromotionPreviewItem> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return MallPromotionPreview.Empty();
        }

        var quantityBySku = items
            .GroupBy(item => item.SkuId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        var skuRows = await (
                from sku in _dbContext.MallSkus
                join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                where quantityBySku.Keys.Contains(sku.Id)
                select new { Sku = sku, Product = product })
            .ToListAsync(cancellationToken);

        // 下架、删除、库存不足的商品不再静默过滤：直接给出明确错误，避免预览金额与可下单金额口径不一致。
        foreach (var skuId in quantityBySku.Keys)
        {
            var row = skuRows.FirstOrDefault(item => item.Sku.Id == skuId);
            if (row is null)
            {
                throw new BusinessException(400, "商品规格不存在或已删除，请刷新后重试");
            }

            if (!row.Sku.IsActive || !row.Product.IsActive)
            {
                throw new BusinessException(400, $"商品「{row.Product.Name}」已下架");
            }

            if (row.Sku.Stock < quantityBySku[skuId])
            {
                throw new BusinessException(400, $"规格「{row.Sku.SpecName}」库存不足");
            }
        }

        var goodsAmount = skuRows.Sum(row => row.Sku.Price * quantityBySku[row.Sku.Id]);

        var promotionContext = await _contextBuilder.BuildAsync(
            memberId,
            skuRows
                .Select(row => new MallPromotionSkuInput(
                    row.Sku,
                    row.Product,
                    quantityBySku[row.Sku.Id]))
                .ToList(),
            cancellationToken);
        if (promotionContext.ActivePromotions.Count == 0)
        {
            return new MallPromotionPreview(
                goodsAmount, 0, new List<MallPromotionPreviewLine>(), promotionContext.OrderContext.Items);
        }

        var matched = _promotionEngine.Match(
            promotionContext.ActivePromotions,
            promotionContext.ScopeMap,
            promotionContext.OrderContext);
        if (matched.Count == 0)
        {
            return new MallPromotionPreview(
                goodsAmount, 0, new List<MallPromotionPreviewLine>(), promotionContext.OrderContext.Items);
        }

        // 与下单同一条路径：一次性应用全部命中促销得出总额，保证预览金额与订单金额一致。
        var baseItems = promotionContext.OrderContext.Items;
        var totalResult = _promotionEngine.Apply(baseItems, matched);
        // 促销合计直接取引擎按动作类型记录的优惠汇总，保证分项之和与合计始终一致
        var totalDiscount = totalResult.PromotionDiscountAmount;

        // 明细直接取引擎记录的行级实际优惠：只有真正生效的促销才会出现在这里，
        // 满减按覆盖范围内各行实际分摊金额汇总，天然与总额一致。
        var lines = totalResult.Items
            .SelectMany(item => item.AppliedPromotions)
            .GroupBy(record => record.PromotionId)
            .Select(group => new MallPromotionPreviewLine
            {
                PromotionId = group.Key,
                Name = group.First().PromotionName,
                DiscountAmount = group.Sum(record => record.DiscountAmount)
            })
            .Where(line => line.DiscountAmount > 0)
            .ToList();

        return new MallPromotionPreview(
            goodsAmount,
            totalDiscount,
            lines,
            totalResult.Items,
            totalResult.IsFreightFree,
            totalResult.RateDiscountAmount,
            totalResult.AmountDiscountAmount,
            totalResult.GiftSkus);
    }

    /// <summary>解析赠品 SKU 的商品信息，供购物车与订单确认页在下单前展示赠品清单。</summary>
    public async Task<List<MallPromotionGiftItem>> ResolveGiftItemsAsync(
        IReadOnlyCollection<GiftSkuRow> gifts,
        CancellationToken cancellationToken)
    {
        if (gifts.Count == 0)
        {
            return new List<MallPromotionGiftItem>();
        }

        var skuIds = gifts.Select(gift => gift.SkuId).Distinct().ToList();
        var skuRows = await (
                from sku in _dbContext.MallSkus
                join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                where skuIds.Contains(sku.Id)
                select new { Sku = sku, Product = product })
            .ToListAsync(cancellationToken);

        var result = new List<MallPromotionGiftItem>();
        foreach (var gift in gifts)
        {
            var row = skuRows.FirstOrDefault(item => item.Sku.Id == gift.SkuId);
            // 已删除或下架的赠品下单时不会发放，预览也不展示，避免误导
            if (row is null || !row.Sku.IsActive || !row.Product.IsActive)
            {
                continue;
            }

            result.Add(new MallPromotionGiftItem(
                row.Sku.Id,
                row.Product.Id,
                row.Product.Name,
                row.Product.MainImageUrl,
                row.Sku.SpecName,
                gift.Quantity,
                gift.PromotionId,
                gift.PromotionName));
        }

        return result;
    }
}

public sealed record MallPromotionPreviewItem(Guid SkuId, int Quantity);

/// <summary>赠品展示信息：0 元计价，仅用于下单前清单展示。</summary>
public sealed record MallPromotionGiftItem(
    Guid SkuId,
    Guid ProductId,
    string ProductName,
    string ProductImage,
    string SpecName,
    int Quantity,
    Guid PromotionId,
    string PromotionName);

public sealed class MallPromotionPreview
{
    public MallPromotionPreview(
        decimal goodsAmount,
        decimal discountAmount,
        List<MallPromotionPreviewLine> lines,
        IReadOnlyList<SkuOrderRow>? promotionItems = null,
        bool isFreightFree = false,
        decimal rateDiscountAmount = 0,
        decimal amountDiscountAmount = 0,
        IReadOnlyList<GiftSkuRow>? giftSkus = null)
    {
        GoodsAmount = goodsAmount;
        DiscountAmount = discountAmount;
        RateDiscountAmount = rateDiscountAmount;
        AmountDiscountAmount = amountDiscountAmount;
        Lines = lines;
        PromotionItems = promotionItems ?? Array.Empty<SkuOrderRow>();
        IsFreightFree = isFreightFree;
        GiftSkus = giftSkus ?? Array.Empty<GiftSkuRow>();
    }

    public decimal GoodsAmount { get; }

    public decimal DiscountAmount { get; }

    /// <summary>折后优惠（DiscountRate）金额。</summary>
    public decimal RateDiscountAmount { get; }

    /// <summary>满减优惠（DiscountAmount）金额。</summary>
    public decimal AmountDiscountAmount { get; }

    public List<MallPromotionPreviewLine> Lines { get; }

    /// <summary>应用促销后的商品行（含折后价），供优惠券按“促销后金额”计算抵扣。</summary>
    public IReadOnlyList<SkuOrderRow> PromotionItems { get; }

    /// <summary>命中的促销是否包含免运费权益，与下单引擎口径一致。</summary>
    public bool IsFreightFree { get; }

    /// <summary>命中的赠品（0 元，不参与金额计算），供下单前展示。</summary>
    public IReadOnlyList<GiftSkuRow> GiftSkus { get; }

    public static MallPromotionPreview Empty() => new(0, 0, new List<MallPromotionPreviewLine>());
}

public sealed class MallPromotionPreviewLine
{
    public Guid PromotionId { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal DiscountAmount { get; set; }
}
