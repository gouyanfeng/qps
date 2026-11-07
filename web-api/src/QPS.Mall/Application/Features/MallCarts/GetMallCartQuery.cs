using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallProducts;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;

namespace QPS.Mall.Application.Features.MallCarts;

public class GetMallCartQuery : IRequest<MallCartDto>
{
    public Guid UserId { get; set; }

    /// <summary>当前选中的 SKU；为 null 表示全部选中。</summary>
    public IReadOnlyCollection<Guid>? SelectedSkuIds { get; set; }
}

public class GetMallCartHandler : IRequestHandler<GetMallCartQuery, MallCartDto>
{
    private readonly IMallDbContext _dbContext;
    private readonly MallPromotionPreviewService _promotionPreview;
    private readonly MallProductPromotionEnricher _productEnricher;

    public GetMallCartHandler(
        IMallDbContext dbContext,
        MallPromotionPreviewService promotionPreview,
        MallProductPromotionEnricher productEnricher)
    {
        _dbContext = dbContext;
        _promotionPreview = promotionPreview;
        _productEnricher = productEnricher;
    }

    public async Task<MallCartDto> Handle(
        GetMallCartQuery request,
        CancellationToken cancellationToken)
    {
        var cartItems = await _dbContext.MallCartItems
            .Where(i => i.UserId == request.UserId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        if (cartItems.Count == 0)
        {
            return new MallCartDto
            {
                TotalQuantity = 0,
                TotalAmount = 0,
                Items = new List<MallCartItemDto>()
            };
        }

        var skuIds = cartItems.Select(i => i.SkuId).Distinct().ToList();
        var skuRows = (await (
                from sku in _dbContext.MallSkus
                join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                where skuIds.Contains(sku.Id)
                select new { Sku = sku, Product = product })
            .ToListAsync(cancellationToken))
            .Select(row => new CartSkuRow(row.Sku, row.Product))
            .ToList();

        var skuMap = skuRows.ToDictionary(row => row.Sku.Id);

        // 购物车行复用商品列表的促销富化逻辑，角标与折后价口径保持一致
        var enrichmentProducts = skuRows
            .GroupBy(row => row.Product.Id)
            .Select(group => new MallProductDto
            {
                Id = group.Key,
                CategoryId = group.First().Product.CategoryId,
                IsActive = group.First().Product.IsActive,
                Skus = group.Select(row => new MallSkuDto
                {
                    Id = row.Sku.Id,
                    ProductId = group.Key,
                    Price = row.Sku.Price,
                    IsActive = row.Sku.IsActive
                }).ToList()
            })
            .ToList();
        await _productEnricher.EnrichAsync(enrichmentProducts, cancellationToken);
        var enrichedProducts = enrichmentProducts.ToDictionary(product => product.Id);
        var enrichedSkus = enrichmentProducts
            .SelectMany(product => product.Skus)
            .ToDictionary(sku => sku.Id);

        var items = new List<MallCartItemDto>();
        var selectedPromotionItems = new List<MallPromotionPreviewItem>();
        var totalAmount = 0m;
        var selectedAmount = 0m;
        var totalQuantity = 0;

        foreach (var cartItem in cartItems)
        {
            var isSelected = request.SelectedSkuIds is null || request.SelectedSkuIds.Contains(cartItem.SkuId);

            if (skuMap.TryGetValue(cartItem.SkuId, out var row))
            {
                var lineAmount = row.Sku.Price * cartItem.Quantity;
                totalAmount += lineAmount;
                totalQuantity += cartItem.Quantity;

                var available = Math.Min(row.Sku.Stock, cartItem.Quantity);
                var isValid = row.Sku.IsActive && row.Product.IsActive && row.Sku.Stock >= cartItem.Quantity;
                if (isSelected && isValid)
                {
                    selectedAmount += lineAmount;
                    selectedPromotionItems.Add(new MallPromotionPreviewItem(cartItem.SkuId, cartItem.Quantity));
                }

                items.Add(new MallCartItemDto
                {
                    Id = cartItem.Id,
                    SkuId = cartItem.SkuId,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    ProductName = row.Product.Name,
                    ProductCode = row.Product.Code,
                    ProductImage = row.Product.MainImageUrl,
                    SkuCode = row.Sku.SkuCode,
                    SpecName = row.Sku.SpecName,
                    Price = row.Sku.Price,
                    DiscountedPrice = enrichedSkus.TryGetValue(row.Sku.Id, out var enrichedSku) &&
                        enrichedSku.DiscountedPrice.HasValue
                            ? enrichedSku.DiscountedPrice.Value
                            : row.Sku.Price,
                    DiscountLabel = enrichedSku?.DiscountLabel,
                    PromotionBadgeText = enrichedProducts.TryGetValue(row.Product.Id, out var enrichedProduct)
                        ? enrichedProduct.PromotionBadgeText
                        : null,
                    PromotionBadgeColor = enrichedProduct?.PromotionBadgeColor,
                    Stock = row.Sku.Stock,
                    WeightGram = row.Sku.WeightGram,
                    AvailableStock = available,
                    LineAmount = lineAmount,
                    IsValid = isValid,
                    InvalidReason = !row.Sku.IsActive || !row.Product.IsActive
                        ? "商品已下架"
                        : row.Sku.Stock < cartItem.Quantity
                            ? $"库存不足（仅剩 {row.Sku.Stock}）"
                            : null
                });
            }
            else
            {
                items.Add(new MallCartItemDto
                {
                    Id = cartItem.Id,
                    SkuId = cartItem.SkuId,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    IsValid = false,
                    InvalidReason = "商品已删除"
                });
            }
        }

        var preview = await _promotionPreview.PreviewAsync(
            request.UserId,
            selectedPromotionItems,
            cancellationToken);
        // 按选中商品试算出赠品，让用户在购物车阶段就知道会送什么
        var gifts = await _promotionPreview.ResolveGiftItemsAsync(
            preview.GiftSkus,
            cancellationToken);

        // 购物车行价按本次试算的实际折扣回填：商品列表展示可忽略订单累计门槛，
        // 但购物车必须与结算一致（未达门槛的折扣不能在行价上先落地）。
        var previewRatePrices = BuildPreviewRatePrices(preview.PromotionItems);
        foreach (var item in items)
        {
            if (!previewRatePrices.TryGetValue(item.SkuId, out var ratePrice)) continue;
            item.DiscountedPrice = ratePrice;
            item.DiscountLabel = ratePrice < item.Price
                ? $"{ratePrice / item.Price * 10:0.#}折"
                : null;
        }

        return new MallCartDto
        {
            TotalQuantity = totalQuantity,
            TotalAmount = totalAmount,
            SelectedAmount = selectedAmount,
            PromotionDiscountAmount = preview.DiscountAmount,
            RateDiscountAmount = preview.RateDiscountAmount,
            AmountDiscountAmount = preview.AmountDiscountAmount,
            PayableAmount = Math.Max(0, selectedAmount - preview.DiscountAmount),
            PromotionDetails = preview.Lines
                .Select(line => new MallCartPromotionDto
                {
                    PromotionId = line.PromotionId,
                    Name = line.Name,
                    DiscountAmount = line.DiscountAmount
                })
                .ToList(),
            Gifts = gifts.Select(gift => new MallPromotionGiftDto
            {
                SkuId = gift.SkuId,
                ProductId = gift.ProductId,
                ProductName = gift.ProductName,
                ProductImage = gift.ProductImage,
                SpecName = gift.SpecName,
                Quantity = gift.Quantity,
                PromotionId = gift.PromotionId,
                PromotionName = gift.PromotionName
            }).ToList(),
            Items = items
        };
    }

    /// <summary>按试算结果还原每行折扣单价：只取 DiscountRate 行级优惠，满减仍留在订单汇总。</summary>
    private static Dictionary<Guid, decimal> BuildPreviewRatePrices(IReadOnlyList<SkuOrderRow> rows)
    {
        return rows
            .Where(row => row.Quantity > 0)
            .Select(row => new
            {
                row.SkuId,
                Price = Math.Round(
                    row.OriginalPrice - row.AppliedPromotions
                        .Where(record => record.ActionType == "DiscountRate")
                        .Sum(record => record.DiscountAmount) / row.Quantity,
                    2)
            })
            .ToDictionary(item => item.SkuId, item => item.Price);
    }

    private sealed record CartSkuRow(MallSku Sku, MallProduct Product);
}
