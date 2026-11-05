using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;

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
                where quantityBySku.Keys.Contains(sku.Id) && sku.IsActive && product.IsActive
                select new { Sku = sku, Product = product })
            .ToListAsync(cancellationToken);

        skuRows = skuRows
            .Where(row => row.Sku.Stock >= quantityBySku[row.Sku.Id])
            .ToList();
        if (skuRows.Count == 0)
        {
            return MallPromotionPreview.Empty();
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
            return new MallPromotionPreview(goodsAmount, 0, new List<MallPromotionPreviewLine>());
        }

        var matched = _promotionEngine.Match(
            promotionContext.ActivePromotions,
            promotionContext.ScopeMap,
            promotionContext.OrderContext);
        if (matched.Count == 0)
        {
            return new MallPromotionPreview(goodsAmount, 0, new List<MallPromotionPreviewLine>());
        }

        // 逐个促销串联应用，用应用前后的金额差额得到每条促销的优惠，总额与明细天然一致。
        var lines = new List<MallPromotionPreviewLine>();
        var runningItems = promotionContext.OrderContext.Items;
        foreach (var matchedPromotion in matched)
        {
            var beforeAmount = runningItems.Sum(item => item.CurrentPrice * item.Quantity);
            var stepResult = _promotionEngine.Apply(
                runningItems,
                new List<MatchedPromotion> { matchedPromotion });
            var afterAmount = stepResult.Items.Sum(item => item.CurrentPrice * item.Quantity);
            if (afterAmount < beforeAmount)
            {
                lines.Add(new MallPromotionPreviewLine
                {
                    PromotionId = matchedPromotion.Promotion.Id,
                    Name = matchedPromotion.Promotion.Name,
                    DiscountAmount = beforeAmount - afterAmount
                });
            }

            runningItems = stepResult.Items;
        }

        return new MallPromotionPreview(
            goodsAmount,
            lines.Sum(line => line.DiscountAmount),
            lines);
    }
}

public sealed record MallPromotionPreviewItem(Guid SkuId, int Quantity);

public sealed class MallPromotionPreview
{
    public MallPromotionPreview(decimal goodsAmount, decimal discountAmount, List<MallPromotionPreviewLine> lines)
    {
        GoodsAmount = goodsAmount;
        DiscountAmount = discountAmount;
        Lines = lines;
    }

    public decimal GoodsAmount { get; }

    public decimal DiscountAmount { get; }

    public List<MallPromotionPreviewLine> Lines { get; }

    public static MallPromotionPreview Empty() => new(0, 0, new List<MallPromotionPreviewLine>());
}

public sealed class MallPromotionPreviewLine
{
    public Guid PromotionId { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal DiscountAmount { get; init; }
}
