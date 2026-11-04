using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallFreightRules;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.Shared.Exceptions;
using System.Text.Json;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 外部下单：校验库存、扣减库存、按运费模板分组计算运费并生成订单。
/// </summary>
public class CreateMallOrderCommand : IRequest<MallOrderDetailDto>
{
    public CreateMallOrderRequest Request { get; set; } = null!;
}

public class CreateMallOrderHandler : IRequestHandler<CreateMallOrderCommand, MallOrderDetailDto>
{
    private const int ReceiverNameMaxLength = 50;
    private const int ReceiverPhoneMaxLength = 30;
    private const int ReceiverAddressMaxLength = 300;
    private const int RemarkMaxLength = 500;

    private readonly IMallDbContext _dbContext;
    private readonly MallPromotionEngine _promotionEngine;

    public CreateMallOrderHandler(
        IMallDbContext dbContext,
        MallPromotionEngine promotionEngine)
    {
        _dbContext = dbContext;
        _promotionEngine = promotionEngine;
    }

    public async Task<MallOrderDetailDto> Handle(
        CreateMallOrderCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        var receiverName = NormalizeRequired(request.ReceiverName, ReceiverNameMaxLength, "收货人姓名");
        var receiverPhone = NormalizeRequired(request.ReceiverPhone, ReceiverPhoneMaxLength, "收货人电话");

        if (string.IsNullOrWhiteSpace(request.ProvinceName) || string.IsNullOrWhiteSpace(request.CityName))
        {
            throw new BusinessException(400, "请选择完整的收货省市区");
        }

        var receiverAddress = NormalizeRequired(request.ReceiverAddress, ReceiverAddressMaxLength, "收货地址");
        var remark = request.Remark?.Trim() ?? string.Empty;

        if (remark.Length > RemarkMaxLength)
        {
            throw new BusinessException(400, $"订单备注不能超过 {RemarkMaxLength} 个字符");
        }

        if (request.Items.Count == 0)
        {
            throw new BusinessException(400, "请至少提交一个商品规格");
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            throw new BusinessException(400, "商品数量必须大于 0");
        }

        if (request.Items.Select(item => item.SkuId).Distinct().Count() != request.Items.Count)
        {
            throw new BusinessException(400, "同一规格不能重复提交");
        }

        var defaultTemplate = await _dbContext.MallFreightTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsDefault && t.IsActive && !t.IsDeleted, cancellationToken);

        var skuIds = request.Items.Select(item => item.SkuId).ToList();

        var skuRows = await (
                from sku in _dbContext.MallSkus
                join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                where skuIds.Contains(sku.Id) && !sku.IsDeleted && !product.IsDeleted
                select new { Sku = sku, Product = product })
            .ToListAsync(cancellationToken);

        var skuMap = skuRows.ToDictionary(row => row.Sku.Id);

        foreach (var item in request.Items)
        {
            if (!skuMap.TryGetValue(item.SkuId, out var row))
            {
                throw new BusinessException(404, "商品规格不存在");
            }

            if (!row.Sku.IsActive || !row.Product.IsActive)
            {
                throw new BusinessException(400, $"商品「{row.Product.Name}」已下架");
            }

            row.Sku.EnsureStockEnough(item.Quantity);
        }

        var originalGoodsAmount = 0m;
        var totalWeightGram = 0;
        var totalVolumeCm3 = 0;

        foreach (var item in request.Items)
        {
            var row = skuMap[item.SkuId];
            originalGoodsAmount += row.Sku.Price * item.Quantity;
            totalWeightGram += row.Sku.WeightGram * item.Quantity;
            totalVolumeCm3 += row.Sku.VolumeCm3 * item.Quantity;
        }

        var now = DateTime.Now;
        var userOrderCount = request.MemberId.HasValue
            ? await _dbContext.MallOrders.CountAsync(
                order => order.MemberId == request.MemberId.Value &&
                         order.Status != MallOrderStatus.Cancelled,
                cancellationToken)
            : 0;

        var activePromotions = await _dbContext.MallPromotions
            .AsNoTracking()
            .WhereEffective(_dbContext, now)
            .ToListAsync(cancellationToken);

        var promotionIds = activePromotions.Select(p => p.Id).ToList();
        var promotionScopeMap = promotionIds.Count == 0
            ? new Dictionary<Guid, List<MallPromotionScope>>()
            : await _dbContext.MallPromotionScopes
                .Where(s => promotionIds.Contains(s.PromotionId) && !s.IsDeleted)
                .GroupBy(s => s.PromotionId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken);

        var productCategoryIds = skuMap.Values
            .Where(row => row.Product.CategoryId.HasValue)
            .Select(row => row.Product.CategoryId!.Value)
            .Distinct()
            .ToArray();
        var categoryParents = await _dbContext.MallCategories
            .Where(category => productCategoryIds.Contains(category.Id))
            .Select(category => new { category.Id, category.ParentId })
            .ToDictionaryAsync(category => category.Id, category => category.ParentId, cancellationToken);

        var orderCtx = new OrderContext
        {
            Now = now,
            UserId = request.MemberId,
            UserOrderCount = userOrderCount,
            IsFirstOrder = request.MemberId.HasValue && userOrderCount == 0,
            GoodsAmount = originalGoodsAmount,
            Items = request.Items.Select(item =>
            {
                var row = skuMap[item.SkuId];
                var categoryIds = new List<Guid>();
                if (row.Product.CategoryId.HasValue)
                {
                    var categoryId = row.Product.CategoryId.Value;
                    categoryIds.Add(categoryId);
                    if (categoryParents.TryGetValue(categoryId, out var parentId) && parentId.HasValue)
                    {
                        categoryIds.Add(parentId.Value);
                    }
                }

                return new SkuOrderRow
                {
                    SkuId = row.Sku.Id,
                    ProductId = row.Product.Id,
                    CategoryId = row.Product.CategoryId,
                    CategoryIds = categoryIds,
                    OriginalPrice = row.Sku.Price,
                    CurrentPrice = row.Sku.Price,
                    Quantity = item.Quantity,
                    WeightGram = row.Sku.WeightGram
                };
            }).ToList()
        };

        if (orderCtx.UserId.HasValue && activePromotions.Count > 0)
        {
            var usagePromotionIds = activePromotions.Select(p => p.Id).ToList();
            var usages = await _dbContext.MallPromotionUsages
                .AsNoTracking()
                .Where(u => usagePromotionIds.Contains(u.PromotionId) && u.UserId == orderCtx.UserId.Value && !u.IsDeleted)
                .ToListAsync(cancellationToken);
            orderCtx.PromotionUsageMap = usages.ToDictionary(u => u.PromotionId, u => u.UsageCount);
        }

        // ===== 换购项（适用源=订单价格的 TradeIn 促销，用户在结算页主动选择）=====
        // 换购金额不计入促销触发门槛（originalGoodsAmount 仅统计正常商品原价）。
        var exchangeRows = new List<ExchangeOrderRow>();
        if (request.ExchangeItems.Count > 0)
        {
            if (request.ExchangeItems.Select(e => e.PromotionId).Distinct().Count() != request.ExchangeItems.Count)
            {
                throw new BusinessException(400, "同一换购活动不能重复提交");
            }

            var exchangePromotionIds = request.ExchangeItems.Select(e => e.PromotionId).ToList();
            var exchangePromotions = await _dbContext.MallPromotions
                .AsNoTracking()
                .Where(p => exchangePromotionIds.Contains(p.Id))
                .WhereEffective(_dbContext, now)
                .ToListAsync(cancellationToken);

            foreach (var exchangeItem in request.ExchangeItems)
            {
                var promotion = exchangePromotions.FirstOrDefault(p => p.Id == exchangeItem.PromotionId);
                if (promotion == null)
                {
                    throw new BusinessException(400, "换购活动不存在或已结束");
                }
                if (promotion.ActionType != "TradeIn" || promotion.ScopeType != "Order" || promotion.TriggerType != "FullAmount")
                {
                    throw new BusinessException(400, "无效的换购活动");
                }

                if (promotion.MaxUsagePerUser > 0
                    && orderCtx.UserId.HasValue
                    && orderCtx.PromotionUsageMap.TryGetValue(promotion.Id, out var exchangeUsed)
                    && exchangeUsed >= promotion.MaxUsagePerUser)
                {
                    throw new BusinessException(400, $"换购活动「{promotion.Name}」已达每人限用次数");
                }

                decimal minAmount = promotion.ThresholdAmount;
                decimal tradeInPrice;
                int maxQuantity;
                Guid configSkuId;
                try
                {
                    using var exchangeActionDoc = JsonDocument.Parse(promotion.ActionConfigJson);
                    var exchangeConfig = exchangeActionDoc.RootElement;
                    if (!exchangeConfig.TryGetProperty("skuId", out var exchangeSkuElem) ||
                        !Guid.TryParse(exchangeSkuElem.GetString(), out configSkuId) ||
                        !exchangeConfig.TryGetProperty("tradeInPrice", out var tradeInPriceElem) ||
                        !exchangeConfig.TryGetProperty("quantity", out var maxQuantityElem))
                    {
                        throw new BusinessException(400, "换购活动配置无效");
                    }
                    tradeInPrice = tradeInPriceElem.GetDecimal();
                    maxQuantity = maxQuantityElem.GetInt32();
                }
                catch (JsonException)
                {
                    throw new BusinessException(400, "换购活动配置不是有效 JSON");
                }

                if (tradeInPrice <= 0 || minAmount <= 0)
                {
                    throw new BusinessException(400, "换购价格或门槛配置无效");
                }
                if (originalGoodsAmount < minAmount)
                {
                    throw new BusinessException(400, $"订单金额未达到换购活动「{promotion.Name}」门槛（满 ¥{minAmount:0.##} 可换购）");
                }
                if (exchangeItem.SkuId != configSkuId)
                {
                    throw new BusinessException(400, "换购规格与活动配置不一致");
                }
                if (exchangeItem.Quantity < 1 || exchangeItem.Quantity > maxQuantity)
                {
                    throw new BusinessException(400, $"换购数量须在 1 到 {maxQuantity} 件之间");
                }

                exchangeRows.Add(new ExchangeOrderRow(promotion, exchangeItem.SkuId, tradeInPrice, exchangeItem.Quantity));
            }

            // 换购规格校验（跟踪查询，后续扣库存需写入状态）与计重。
            var exchangeSkuIds = exchangeRows.Select(e => e.SkuId).Distinct().ToList();
            var exchangeSkuRows = await (
                    from sku in _dbContext.MallSkus
                    join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                    where exchangeSkuIds.Contains(sku.Id) && !sku.IsDeleted && !product.IsDeleted
                    select new { Sku = sku, Product = product })
                .ToListAsync(cancellationToken);
            var exchangeSkuMap = exchangeSkuRows.ToDictionary(row => row.Sku.Id);

            foreach (var exchange in exchangeRows)
            {
                if (!exchangeSkuMap.TryGetValue(exchange.SkuId, out var exchangeRow))
                {
                    throw new BusinessException(400, "换购商品规格不存在");
                }
                if (!exchangeRow.Sku.IsActive || !exchangeRow.Product.IsActive)
                {
                    throw new BusinessException(400, $"换购商品「{exchangeRow.Product.Name}」已下架");
                }

                exchangeRow.Sku.EnsureStockEnough(exchange.Quantity);
                totalWeightGram += exchangeRow.Sku.WeightGram * exchange.Quantity;
                totalVolumeCm3 += exchangeRow.Sku.VolumeCm3 * exchange.Quantity;
                exchange.Bind(exchangeRow.Sku, exchangeRow.Product);
            }
        }

        var matchedPromotions = _promotionEngine.Match(activePromotions, promotionScopeMap, orderCtx);
        var applyResult = _promotionEngine.Apply(orderCtx.Items, matchedPromotions);

        var goodsAmount = applyResult.Items.Sum(i => i.CurrentPrice * i.Quantity)
            + exchangeRows.Sum(e => e.TradeInPrice * e.Quantity);

        var templateIds = new HashSet<Guid>();
        if (defaultTemplate is not null)
        {
            templateIds.Add(defaultTemplate.Id);
        }
        foreach (var row in skuMap.Values)
        {
            if (row.Product.FreightTemplateId.HasValue)
            {
                templateIds.Add(row.Product.FreightTemplateId.Value);
            }
            else if (defaultTemplate is not null)
            {
                templateIds.Add(defaultTemplate.Id);
            }
        }

        var templateMap = new Dictionary<Guid, MallFreightTemplate>();
        var regionMap = new Dictionary<Guid, List<MallFreightRegion>>();

        if (templateIds.Count > 0)
        {
            var templates = await _dbContext.MallFreightTemplates
                .AsNoTracking()
                .Where(t => templateIds.Contains(t.Id) && !t.IsDeleted)
                .ToListAsync(cancellationToken);

            templateMap = templates.ToDictionary(t => t.Id);

            var regions = await _dbContext.MallFreightRegions
                .AsNoTracking()
                .Where(r => templateIds.Contains(r.TemplateId) && !r.IsDeleted)
                .ToListAsync(cancellationToken);

            regionMap = regions.GroupBy(r => r.TemplateId)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.IsDefaultRegion).ToList());
        }

        var freightItems = new List<FreightItemRow>();
        foreach (var appliedItem in applyResult.Items)
        {
            var row = skuMap[appliedItem.SkuId];
            freightItems.Add(new FreightItemRow
            {
                SkuId = row.Sku.Id,
                ProductId = row.Product.Id,
                FreightTemplateId = row.Product.FreightTemplateId,
                Quantity = appliedItem.Quantity,
                WeightGram = row.Sku.WeightGram,
                VolumeCm3 = row.Sku.VolumeCm3,
                LineAmount = appliedItem.CurrentPrice * appliedItem.Quantity
            });
        }

        foreach (var exchange in exchangeRows)
        {
            freightItems.Add(new FreightItemRow
            {
                SkuId = exchange.Sku!.Id,
                ProductId = exchange.Product!.Id,
                FreightTemplateId = exchange.Product.FreightTemplateId,
                Quantity = exchange.Quantity,
                WeightGram = exchange.Sku.WeightGram,
                VolumeCm3 = exchange.Sku.VolumeCm3,
                LineAmount = exchange.TradeInPrice * exchange.Quantity
            });
        }

        decimal freightAmount;
        if (defaultTemplate is not null || skuMap.Values.Any(r => r.Product.FreightTemplateId.HasValue))
        {
            var addressInfo = new FreightAddressInfo
            {
                ProvinceName = request.ProvinceName,
                CityName = request.CityName
            };

            var calcResult = MallFreightCalculator.Calculate(
                freightItems,
                defaultTemplate?.Id ?? Guid.Empty,
                templateMap,
                regionMap,
                addressInfo);

            freightAmount = calcResult.TotalFreight;
        }
        else
        {
            throw new BusinessException(400, "未配置默认运费模板，无法下单");
        }

        if (applyResult.IsFreightFree)
        {
            freightAmount = 0;
        }

        var fullReceiverAddress = $"{request.ProvinceName}{request.CityName}{request.DistrictName}{receiverAddress}";

        var orderNo = string.IsNullOrWhiteSpace(request.OrderNo)
            ? MallNumberGenerator.Generate("ORD")
            : request.OrderNo.Trim();

        var orderNoExists = await _dbContext.MallOrders
            .AnyAsync(order => order.OrderNo == orderNo && !order.IsDeleted, cancellationToken);

        if (orderNoExists)
        {
            throw new BusinessException(400, "订单号已存在");
        }

        var order = MallOrder.Create(
            orderNo,
            request.MemberId,
            receiverName,
            receiverPhone,
            fullReceiverAddress,
            goodsAmount,
            freightAmount,
            totalWeightGram,
            remark);

        _dbContext.MallOrders.Add(order);

        foreach (var appliedItem in applyResult.Items)
        {
            var row = skuMap[appliedItem.SkuId];
            var sku = row.Sku;
            var beforeStock = sku.DeductStock(appliedItem.Quantity);

            _dbContext.MallStockLogs.Add(MallStockLog.Create(
                sku.Id,
                sku.SkuCode,
                sku.SpecName,
                MallStockChangeType.OrderDeduct,
                beforeStock,
                sku.Stock,
                relatedOrderNo: orderNo));

            Guid? promotionId = null;
            string promotionName = string.Empty;
            decimal? originalUnitPrice = null;

            if (appliedItem.IsDiscounted)
            {
                var matched = matchedPromotions.FirstOrDefault(m =>
                    m.Promotion.Id == appliedItem.AppliedPromotionId);
                if (matched != null)
                {
                    promotionId = matched.Promotion.Id;
                    promotionName = matched.Promotion.Name;
                    originalUnitPrice = appliedItem.OriginalPrice;
                }
            }

            _dbContext.MallOrderItems.Add(MallOrderItem.Create(
                order.Id,
                row.Sku.Id,
                row.Product.Id,
                row.Product.Name,
                row.Sku.SkuCode,
                row.Sku.SpecName,
                appliedItem.CurrentPrice,
                appliedItem.Quantity,
                row.Sku.WeightGram,
                isGift: false,
                promotionId: promotionId,
                promotionName: promotionName,
                originalUnitPrice: originalUnitPrice));
        }

        foreach (var exchange in exchangeRows)
        {
            var exchangeSku = exchange.Sku!;
            var beforeExchangeStock = exchangeSku.DeductStock(exchange.Quantity);

            _dbContext.MallStockLogs.Add(MallStockLog.Create(
                exchangeSku.Id,
                exchangeSku.SkuCode,
                exchangeSku.SpecName,
                MallStockChangeType.OrderDeduct,
                beforeExchangeStock,
                exchangeSku.Stock,
                relatedOrderNo: orderNo));

            _dbContext.MallOrderItems.Add(MallOrderItem.Create(
                order.Id,
                exchangeSku.Id,
                exchange.Product!.Id,
                exchange.Product.Name,
                exchangeSku.SkuCode,
                exchangeSku.SpecName,
                exchange.TradeInPrice,
                exchange.Quantity,
                exchangeSku.WeightGram,
                isGift: false,
                promotionId: exchange.Promotion.Id,
                promotionName: exchange.Promotion.Name,
                originalUnitPrice: exchangeSku.Price));
        }

        if (applyResult.GiftSkus.Count > 0)
        {
            var giftSkuIds = applyResult.GiftSkus.Select(g => g.SkuId).Distinct().ToList();
            var giftSkuRows = await (
                    from sku in _dbContext.MallSkus
                    join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                    where giftSkuIds.Contains(sku.Id) && !sku.IsDeleted && !product.IsDeleted
                    select new { Sku = sku, Product = product })
                .ToListAsync(cancellationToken);

            var giftSkuMap = giftSkuRows.ToDictionary(row => row.Sku.Id);

            foreach (var gift in applyResult.GiftSkus)
            {
                if (!giftSkuMap.TryGetValue(gift.SkuId, out var giftRow))
                {
                    throw new BusinessException(400, "赠品规格不存在");
                }

                var giftSku = giftRow.Sku;
                giftSku.EnsureStockEnough(gift.Quantity);
                var beforeStock = giftSku.DeductStock(gift.Quantity);

                _dbContext.MallStockLogs.Add(MallStockLog.Create(
                    giftSku.Id,
                    giftSku.SkuCode,
                    giftSku.SpecName,
                    MallStockChangeType.OrderDeduct,
                    beforeStock,
                    giftSku.Stock,
                    relatedOrderNo: orderNo));

                _dbContext.MallOrderItems.Add(MallOrderItem.Create(
                    order.Id,
                    giftSku.Id,
                    giftRow.Product.Id,
                    giftRow.Product.Name,
                    giftSku.SkuCode,
                    giftSku.SpecName,
                    0,
                    gift.Quantity,
                    giftSku.WeightGram,
                    isGift: true,
                    promotionId: gift.PromotionId,
                    promotionName: gift.PromotionName));
            }
        }

        if (orderCtx.UserId.HasValue && (matchedPromotions.Count > 0 || exchangeRows.Count > 0))
        {
            var usageTargets = matchedPromotions.Select(m => m.Promotion)
                .Concat(exchangeRows.Select(e => e.Promotion))
                .DistinctBy(p => p.Id)
                .ToList();
            var usagePromotionIds = usageTargets.Select(p => p.Id).ToList();
            var existingUsages = await _dbContext.MallPromotionUsages
                .Where(u => usagePromotionIds.Contains(u.PromotionId) && u.UserId == orderCtx.UserId.Value && !u.IsDeleted)
                .ToListAsync(cancellationToken);

            var usageDict = existingUsages.ToDictionary(u => u.PromotionId);

            foreach (var promotion in usageTargets)
            {
                if (!usageDict.TryGetValue(promotion.Id, out var usage))
                {
                    usage = MallPromotionUsage.Create(promotion.Id, orderCtx.UserId.Value);
                    _dbContext.MallPromotionUsages.Add(usage);
                }
                usage.Increment(now);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MallOrderDetailLoader.LoadAsync(_dbContext, order.Id, cancellationToken);
    }

    private static string NormalizeRequired(string? value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, $"请输入{fieldName}");
        }

        if (normalized.Length > maxLength)
        {
            throw new BusinessException(400, $"{fieldName}不能超过 {maxLength} 个字符");
        }

        return normalized;
    }
}

/// <summary>
/// 已通过校验的换购下单行；Sku/Product 在规格校验后绑定（跟踪查询，用于扣库存）。
/// </summary>
internal sealed class ExchangeOrderRow
{
    public ExchangeOrderRow(MallPromotion promotion, Guid skuId, decimal tradeInPrice, int quantity)
    {
        Promotion = promotion;
        SkuId = skuId;
        TradeInPrice = tradeInPrice;
        Quantity = quantity;
    }

    public MallPromotion Promotion { get; }

    public Guid SkuId { get; }

    public decimal TradeInPrice { get; }

    public int Quantity { get; }

    public MallSku? Sku { get; private set; }

    public MallProduct? Product { get; private set; }

    public void Bind(MallSku sku, MallProduct product)
    {
        Sku = sku;
        Product = product;
    }
}