using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Application.Features.MallProducts;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCarts;

public class GetMallCartQuery : IRequest<MallCartDto>
{
    public Guid UserId { get; set; }

    /// <summary>当前选中的 SKU；为 null 表示全部选中。</summary>
    public IReadOnlyCollection<Guid>? SelectedSkuIds { get; set; }

    /// <summary>
    /// 临时试算行（skuId + 数量），不落购物车库。用于立即购买等不希望污染购物车的入口；
    /// 为 null 时使用购物车中已有的行。数量以这里传入的为准，不与购物车累加。
    /// </summary>
    public IReadOnlyCollection<CartPreviewLine>? PreviewItems { get; set; }

    /// <summary>收货地址 ID；传入时按该地址计算运费，购物车阶段不传（运费 0）。</summary>
    public Guid? AddressId { get; set; }

    /// <summary>会员券 ID；传入时计算券抵扣，购物车阶段不传。</summary>
    public Guid? MemberCouponId { get; set; }
}

public class GetMallCartHandler : IRequestHandler<GetMallCartQuery, MallCartDto>
{
    private readonly IMallDbContext _dbContext;
    private readonly MallPromotionPreviewService _promotionPreview;
    private readonly MallProductPromotionEnricher _productEnricher;
    private readonly MallCheckoutPricingService _checkoutPricing;

    public GetMallCartHandler(
        IMallDbContext dbContext,
        MallPromotionPreviewService promotionPreview,
        MallProductPromotionEnricher productEnricher,
        MallCheckoutPricingService checkoutPricing)
    {
        _dbContext = dbContext;
        _promotionPreview = promotionPreview;
        _productEnricher = productEnricher;
        _checkoutPricing = checkoutPricing;
    }

    public async Task<MallCartDto> Handle(
        GetMallCartQuery request,
        CancellationToken cancellationToken)
    {
        // 临时试算行（立即购买）与购物车行共用后续组装/试算逻辑，但不写库、不与购物车数量累加
        List<CartLine> cartLines;
        if (request.PreviewItems is not null)
        {
            var invalid = request.PreviewItems.FirstOrDefault(i => i.Quantity <= 0);
            if (invalid is not null) throw new BusinessException(400, "数量必须大于 0");
            cartLines = request.PreviewItems
                .Select(i => new CartLine(Guid.Empty, i.SkuId, Guid.Empty, i.Quantity))
                .ToList();
        }
        else
        {
            var cartEntities = await _dbContext.MallCartItems
                .Where(i => i.UserId == request.UserId)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
            cartLines = cartEntities
                .Select(i => new CartLine(i.Id, i.SkuId, i.ProductId, i.Quantity))
                .ToList();
        }

        if (cartLines.Count == 0)
        {
            return new MallCartDto
            {
                TotalQuantity = 0,
                TotalAmount = 0,
                Items = new List<MallCartItemDto>()
            };
        }

        var skuIds = cartLines.Select(i => i.SkuId).Distinct().ToList();
        var skuRows = (await (
                from sku in _dbContext.MallSkus
                join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                join store in _dbContext.MallStores on product.StoreId equals store.Id
                where skuIds.Contains(sku.Id)
                select new { Sku = sku, Product = product, Store = store })
            .ToListAsync(cancellationToken))
            .Select(row => new CartSkuRow(row.Sku, row.Product, row.Store))
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

        foreach (var cartItem in cartLines)
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
                    StoreId = row.Store.Id,
                    StoreName = row.Store.Name,
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
        // 券与运费与结算页共用同一定价服务：购物车阶段不传地址/券，两者为 0
        var pricing = await _checkoutPricing.CalculateAsync(
            request.UserId,
            preview,
            selectedPromotionItems,
            request.AddressId,
            request.MemberCouponId,
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
            CouponDiscountAmount = pricing.CouponDiscountAmount,
            CouponName = pricing.CouponName,
            CouponGapAmount = pricing.CouponGapAmount,
            CouponHint = pricing.CouponHint,
            FreightAmount = pricing.FreightAmount,
            DiscountedGoodsAmount = pricing.Breakdown.DiscountedGoodsAmount,
            PayableAmount = pricing.Breakdown.PayableAmount,
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

    private sealed record CartLine(Guid Id, Guid SkuId, Guid ProductId, int Quantity);
    private sealed record CartSkuRow(MallSku Sku, MallProduct Product, MallStore Store);
}