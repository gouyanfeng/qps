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
/// 创建订单：校验库存、扣减库存、按运费模板计算运费并生成订单。
/// </summary>
public class CreateMallOrderCommand : IRequest<MallOrderDetailDto>
{
    public CreateMallOrderRequest Request { get; set; } = null!;
}

/// <summary>
/// 下单用例编排：Handle 只负责阶段调度，查询、校验、计价与落库分别下沉到私有方法与领域服务。
/// </summary>
public class CreateMallOrderHandler : IRequestHandler<CreateMallOrderCommand, MallOrderDetailDto>
{
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
        // ===== 阶段一：装配 —— 登录与地址快照、入参校验、SKU 与库存预检、促销数据装配 =====
        var context = await BuildOrderContextAsync(command.Request, cancellationToken);

        // ===== 阶段二：促销计价 —— 引擎匹配有效促销并逐行应用（改价/赠品/免运费标记），计算货款 =====
        var (matchedPromotions, applyResult) = ApplyPromotions(context);
        var goodsAmount = CalculateGoodsAmount(applyResult);

        // ===== 阶段三：运费 —— 按商品运费模板（回退默认模板）与收货区域计算，促销免运费时清零 =====
        var freightAmount = await CalculateFreightAsync(context, applyResult, cancellationToken);

        // ===== 阶段四：落库 —— 订单、明细（正常/赠品）、库存扣减、流水与限用计数 =====
        var order = await CreateOrderAsync(
            context, applyResult, matchedPromotions, goodsAmount, freightAmount, cancellationToken);

        // ===== 阶段五：提交 —— 单次 SaveChanges 事务提交，返回完整订单详情 =====
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MallOrderDetailLoader.LoadAsync(_dbContext, order.Id, cancellationToken);
    }

    /// <summary>
    /// 阶段一：登录与地址快照、入参跨行校验、SKU 加载与库存预检、促销数据装配。
    /// </summary>
    private async Task<MallOrderBuildContext> BuildOrderContextAsync(
        CreateMallOrderRequest request,
        CancellationToken cancellationToken)
    {
        var address = await ResolveReceiverAddressAsync(request, cancellationToken);

        // DataAnnotations 已覆盖备注长度、明细非空与数量范围；此处仅保留跨行规则
        if (request.Items.Select(item => item.SkuId).Distinct().Count() != request.Items.Count)
        {
            throw new BusinessException(400, "同一规格不能重复提交");
        }

        var skuMap = await LoadAndValidateSkuMapAsync(request.Items, cancellationToken);
        var totals = SumOrderTotals(request.Items, skuMap);
        var defaultFreightTemplate = await LoadDefaultFreightTemplateAsync(cancellationToken);
        var promotionData = await LoadPromotionDataAsync(request, skuMap, cancellationToken);

        return new MallOrderBuildContext
        {
            Request = request,
            Address = address,
            SkuMap = skuMap,
            Totals = totals,
            DefaultFreightTemplate = defaultFreightTemplate,
            ActivePromotions = promotionData.ActivePromotions,
            PromotionScopeMap = promotionData.ScopeMap,
            PromotionOrderContext = promotionData.OrderContext
        };
    }

    /// <summary>校验登录状态并解析收货地址（归属当前会员），供订单快照使用。</summary>
    private async Task<MallAddress> ResolveReceiverAddressAsync(
        CreateMallOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MemberId is null)
        {
            throw new BusinessException(401, "请先登录后下单");
        }

        var address = await _dbContext.MallAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.AddressId && !item.IsDeleted, cancellationToken);
        if (address is null || address.UserId != request.MemberId)
        {
            throw new BusinessException(400, "收货地址不存在或已删除，请重新选择");
        }

        return address;
    }

    /// <summary>按 SKU ID 集合加载 SKU 与所属商品（跟踪查询，供后续扣库存）。</summary>
    private async Task<Dictionary<Guid, (MallSku Sku, MallProduct Product)>> LoadSkuProductMapAsync(
        IReadOnlyCollection<Guid> skuIds,
        CancellationToken cancellationToken)
    {
        var ids = skuIds.Distinct().ToList();
        var rows = await (
                from sku in _dbContext.MallSkus
                join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                where ids.Contains(sku.Id) && !sku.IsDeleted && !product.IsDeleted
                select new { Sku = sku, Product = product })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Sku.Id, row => (row.Sku, row.Product));
    }

    /// <summary>校验 SKU 行存在且商品/规格均在架；不存在时的错误码与文案由业务来源决定。</summary>
    private static (MallSku Sku, MallProduct Product) EnsureSkuAvailable(
        Dictionary<Guid, (MallSku Sku, MallProduct Product)> skuMap,
        Guid skuId,
        int notFoundCode,
        string notFoundMessage)
    {
        if (!skuMap.TryGetValue(skuId, out var row))
        {
            throw new BusinessException(notFoundCode, notFoundMessage);
        }

        if (!row.Sku.IsActive || !row.Product.IsActive)
        {
            throw new BusinessException(400, $"商品「{row.Product.Name}」已下架");
        }

        return row;
    }

    /// <summary>加载下单 SKU 与所属商品，逐项校验存在、上架与库存；使用跟踪查询供后续扣库存。</summary>
    private async Task<Dictionary<Guid, (MallSku Sku, MallProduct Product)>> LoadAndValidateSkuMapAsync(
        IReadOnlyCollection<CreateMallOrderItemRequest> items,
        CancellationToken cancellationToken)
    {
        var skuMap = await LoadSkuProductMapAsync(items.Select(item => item.SkuId).ToList(), cancellationToken);

        foreach (var item in items)
        {
            var row = EnsureSkuAvailable(skuMap, item.SkuId, 404, "商品规格不存在");
            row.Sku.EnsureStockEnough(item.Quantity);
        }

        return skuMap;
    }

    /// <summary>汇总商品总重量与总体积。</summary>
    private static MallOrderTotals SumOrderTotals(
        IReadOnlyCollection<CreateMallOrderItemRequest> items,
        Dictionary<Guid, (MallSku Sku, MallProduct Product)> skuMap)
    {
        var totals = new MallOrderTotals();
        foreach (var item in items)
        {
            var row = skuMap[item.SkuId];
            totals.TotalWeightGram += row.Sku.WeightGram * item.Quantity;
            totals.TotalVolumeCm3 += row.Sku.VolumeCm3 * item.Quantity;
        }

        return totals;
    }

    private async Task<MallFreightTemplate?> LoadDefaultFreightTemplateAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.MallFreightTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsDefault && t.IsActive && !t.IsDeleted, cancellationToken);
    }

    /// <summary>装配促销阶段数据：有效促销、范围表与促销引擎订单上下文。</summary>
    private async Task<MallPromotionData> LoadPromotionDataAsync(
        CreateMallOrderRequest request,
        Dictionary<Guid, (MallSku Sku, MallProduct Product)> skuMap,
        CancellationToken cancellationToken)
    {
        // MemberId 已在 ResolveReceiverAddressAsync 阶段校验非空
        var memberId = request.MemberId!.Value;
        var quantitiesBySku = request.Items
            .GroupBy(item => item.SkuId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var now = DateTime.Now;

        var activePromotions = await _dbContext.MallPromotions
            .AsNoTracking()
            .WhereEffective(now)
            .ToListAsync(cancellationToken);

        var promotionIds = activePromotions.Select(p => p.Id).ToList();
        var scopeMap = promotionIds.Count == 0
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
            UserId = memberId,
            Items = skuMap.Values.Select(row =>
            {
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
                    Quantity = quantitiesBySku.GetValueOrDefault(row.Sku.Id),
                    WeightGram = row.Sku.WeightGram
                };
            }).ToList()
        };

        if (activePromotions.Count > 0)
        {
            var usagePromotionIds = activePromotions.Select(p => p.Id).ToList();
            var usages = await _dbContext.MallPromotionUsages
                .AsNoTracking()
                .Where(u => usagePromotionIds.Contains(u.PromotionId) && u.UserId == memberId && !u.IsDeleted)
                .ToListAsync(cancellationToken);
            orderCtx.PromotionUsageMap = usages.ToDictionary(u => u.PromotionId, u => u.UsageCount);
        }

        return new MallPromotionData
        {
            ActivePromotions = activePromotions,
            ScopeMap = scopeMap,
            OrderContext = orderCtx
        };
    }

    /// <summary>阶段二：促销引擎匹配与应用（逐行改价、免运费标记、赠品清单）。</summary>
    private (List<MatchedPromotion> MatchedPromotions, ApplyResult ApplyResult) ApplyPromotions(
        MallOrderBuildContext context)
    {
        var orderCtx = context.PromotionOrderContext!;
        var matchedPromotions = _promotionEngine.Match(context.ActivePromotions, context.PromotionScopeMap, orderCtx);
        var applyResult = _promotionEngine.Apply(orderCtx.Items, matchedPromotions);
        return (matchedPromotions, applyResult);
    }

    /// <summary>货款 = 促销后商品金额。</summary>
    private static decimal CalculateGoodsAmount(ApplyResult applyResult)
    {
        return applyResult.Items.Sum(i => i.CurrentPrice * i.Quantity);
    }

    /// <summary>阶段三：加载运费模板与区域并计算运费；促销免运费时清零。</summary>
    private async Task<decimal> CalculateFreightAsync(
        MallOrderBuildContext context,
        ApplyResult applyResult,
        CancellationToken cancellationToken)
    {
        await LoadFreightTemplatesAsync(context, cancellationToken);

        var freightItems = BuildFreightItems(context, applyResult);

        if (context.DefaultFreightTemplate is null &&
            !context.SkuMap.Values.Any(row => row.Product.FreightTemplateId.HasValue))
        {
            throw new BusinessException(400, "未配置默认运费模板，无法下单");
        }

        var addressInfo = new FreightAddressInfo
        {
            ProvinceName = context.Address.ProvinceName,
            CityName = context.Address.CityName
        };

        var calcResult = MallFreightCalculator.Calculate(
            freightItems,
            context.DefaultFreightTemplate?.Id ?? Guid.Empty,
            context.FreightTemplateMap,
            context.FreightRegionMap,
            addressInfo);

        return applyResult.IsFreightFree ? 0 : calcResult.TotalFreight;
    }

    /// <summary>阶段三：收集商品涉及的运费模板（商品模板缺失时回退默认模板）并加载模板与区域数据。</summary>
    private async Task LoadFreightTemplatesAsync(
        MallOrderBuildContext context,
        CancellationToken cancellationToken)
    {
        var templateIds = new HashSet<Guid>();
        if (context.DefaultFreightTemplate is not null)
        {
            templateIds.Add(context.DefaultFreightTemplate.Id);
        }

        foreach (var row in context.SkuMap.Values)
        {
            if (row.Product.FreightTemplateId.HasValue)
            {
                templateIds.Add(row.Product.FreightTemplateId.Value);
            }
            else if (context.DefaultFreightTemplate is not null)
            {
                templateIds.Add(context.DefaultFreightTemplate.Id);
            }
        }

        if (templateIds.Count == 0)
        {
            return;
        }

        var templates = await _dbContext.MallFreightTemplates
            .AsNoTracking()
            .Where(t => templateIds.Contains(t.Id) && !t.IsDeleted)
            .ToListAsync(cancellationToken);
        context.FreightTemplateMap = templates.ToDictionary(t => t.Id);

        var regions = await _dbContext.MallFreightRegions
            .AsNoTracking()
            .Where(r => templateIds.Contains(r.TemplateId) && !r.IsDeleted)
            .ToListAsync(cancellationToken);
        context.FreightRegionMap = regions.GroupBy(r => r.TemplateId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.IsDefaultRegion).ToList());
    }

    /// <summary>阶段三：构建运费计算输入行（促销后的商品行）。</summary>
    private static List<FreightItemRow> BuildFreightItems(
        MallOrderBuildContext context,
        ApplyResult applyResult)
    {
        var freightItems = new List<FreightItemRow>();
        foreach (var appliedItem in applyResult.Items)
        {
            var row = context.SkuMap[appliedItem.SkuId];
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

        return freightItems;
    }

    /// <summary>阶段四：生成订单与明细（正常行、赠品行），扣减库存并累加限用计数（随 SaveChanges 一次提交）。</summary>
    private async Task<MallOrder> CreateOrderAsync(
        MallOrderBuildContext context,
        ApplyResult applyResult,
        List<MatchedPromotion> matchedPromotions,
        decimal goodsAmount,
        decimal freightAmount,
        CancellationToken cancellationToken)
    {
        var request = context.Request;
        var orderNo = await GenerateOrderNoAsync(cancellationToken);
        var fullReceiverAddress = context.ResolveFullReceiverAddress();

        var order = MallOrder.Create(
            orderNo,
            request.MemberId,
            context.Address.ReceiverName,
            context.Address.ReceiverPhone,
            fullReceiverAddress,
            goodsAmount,
            freightAmount,
            context.Totals.TotalWeightGram,
            request.Remark?.Trim() ?? string.Empty);

        _dbContext.MallOrders.Add(order);

        AppendProductLines(order, context, applyResult, matchedPromotions);
        await AppendGiftLinesAsync(order, context, applyResult, cancellationToken);
        await IncrementPromotionUsagesAsync(context, matchedPromotions, cancellationToken);

        return order;
    }

    /// <summary>阶段四：生成订单号并确保唯一。</summary>
    private async Task<string> GenerateOrderNoAsync(CancellationToken cancellationToken)
    {
        var orderNo = MallNumberGenerator.Generate("ORD");

        var orderNoExists = await _dbContext.MallOrders
            .AnyAsync(order => order.OrderNo == orderNo && !order.IsDeleted, cancellationToken);
        if (orderNoExists)
        {
            throw new BusinessException(400, "订单号已存在");
        }

        return orderNo;
    }

    /// <summary>阶段四：正常商品行 —— 扣库存、写库存流水与订单明细（命中促销时记录促销来源与原价）。</summary>
    private void AppendProductLines(
        MallOrder order,
        MallOrderBuildContext context,
        ApplyResult applyResult,
        List<MatchedPromotion> matchedPromotions)
    {
        foreach (var appliedItem in applyResult.Items)
        {
            var row = context.SkuMap[appliedItem.SkuId];
            DeductStockAndLog(order, row.Sku, appliedItem.Quantity);

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

            AddOrderItem(order, row.Sku, row.Product,
                appliedItem.CurrentPrice, appliedItem.Quantity,
                isGift: false, promotionId, promotionName, originalUnitPrice);
        }
    }

    /// <summary>阶段四：赠品行 —— 校验库存、扣库存并生成 0 元赠品明细。</summary>
    private async Task AppendGiftLinesAsync(
        MallOrder order,
        MallOrderBuildContext context,
        ApplyResult applyResult,
        CancellationToken cancellationToken)
    {
        if (applyResult.GiftSkus.Count == 0)
        {
            return;
        }

        var giftSkuMap = await LoadSkuProductMapAsync(
            applyResult.GiftSkus.Select(g => g.SkuId).ToList(), cancellationToken);

        foreach (var gift in applyResult.GiftSkus)
        {
            var giftRow = EnsureSkuAvailable(giftSkuMap, gift.SkuId, 400, "赠品规格不存在");
            giftRow.Sku.EnsureStockEnough(gift.Quantity);
            DeductStockAndLog(order, giftRow.Sku, gift.Quantity);

            AddOrderItem(order, giftRow.Sku, giftRow.Product,
                0, gift.Quantity, isGift: true, gift.PromotionId, gift.PromotionName);
        }
    }

    /// <summary>扣减库存并写入订单扣减流水。</summary>
    private void DeductStockAndLog(MallOrder order, MallSku sku, int quantity)
    {
        var beforeStock = sku.DeductStock(quantity);

        _dbContext.MallStockLogs.Add(MallStockLog.Create(
            sku.Id,
            sku.SkuCode,
            sku.SpecName,
            MallStockChangeType.OrderDeduct,
            beforeStock,
            sku.Stock,
            relatedOrderNo: order.OrderNo));
    }

    /// <summary>写入订单商品明细行（价格、赠品标记与促销来源由调用方决定）。</summary>
    private void AddOrderItem(
        MallOrder order,
        MallSku sku,
        MallProduct product,
        decimal unitPrice,
        int quantity,
        bool isGift,
        Guid? promotionId = null,
        string promotionName = "",
        decimal? originalUnitPrice = null)
    {
        _dbContext.MallOrderItems.Add(MallOrderItem.Create(
            order.Id,
            sku.Id,
            product.Id,
            product.Name,
            sku.SkuCode,
            sku.SpecName,
            unitPrice,
            quantity,
            sku.WeightGram,
            isGift,
            promotionId,
            promotionName,
            originalUnitPrice));
    }

    /// <summary>阶段四：命中促销的每人限用计数累加；无记录时新建。</summary>
    private async Task IncrementPromotionUsagesAsync(
        MallOrderBuildContext context,
        List<MatchedPromotion> matchedPromotions,
        CancellationToken cancellationToken)
    {
        var orderCtx = context.PromotionOrderContext;
        if (matchedPromotions.Count == 0)
        {
            return;
        }

        var usageTargets = matchedPromotions.Select(m => m.Promotion)
            .DistinctBy(p => p.Id)
            .ToList();
        var usagePromotionIds = usageTargets.Select(p => p.Id).ToList();
        var existingUsages = await _dbContext.MallPromotionUsages
            .Where(u => usagePromotionIds.Contains(u.PromotionId) && u.UserId == orderCtx.UserId.Value && !u.IsDeleted)
            .ToListAsync(cancellationToken);

        var usageDict = existingUsages.ToDictionary(u => u.PromotionId);
        var now = DateTime.Now;

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
}

/// <summary>
/// 下单装配上下文：承载地址快照、SKU 行、合计与促销数据等跨阶段共享数据。
/// </summary>
internal sealed class MallOrderBuildContext
{
    public required CreateMallOrderRequest Request { get; init; }

    public required MallAddress Address { get; init; }

    public required Dictionary<Guid, (MallSku Sku, MallProduct Product)> SkuMap { get; init; }

    public required MallOrderTotals Totals { get; init; }

    public required List<MallPromotion> ActivePromotions { get; init; }

    public required Dictionary<Guid, List<MallPromotionScope>> PromotionScopeMap { get; init; }

    public required OrderContext PromotionOrderContext { get; init; }

    public MallFreightTemplate? DefaultFreightTemplate { get; set; }

    public Dictionary<Guid, MallFreightTemplate> FreightTemplateMap { get; set; } = new();

    public Dictionary<Guid, List<MallFreightRegion>> FreightRegionMap { get; set; } = new();

    /// <summary>拼装省市区 + 详址的完整收货地址快照。</summary>
    public string ResolveFullReceiverAddress()
    {
        return $"{Address.ProvinceName}{Address.CityName}{Address.DistrictName}{Address.DetailAddress}";
    }
}

/// <summary>订单合计：总重量与总体积。</summary>
internal sealed class MallOrderTotals
{
    public int TotalWeightGram { get; set; }

    public int TotalVolumeCm3 { get; set; }
}

/// <summary>促销阶段装配数据：有效促销、范围表与促销引擎订单上下文。</summary>
internal sealed class MallPromotionData
{
    public required List<MallPromotion> ActivePromotions { get; init; }

    public required Dictionary<Guid, List<MallPromotionScope>> ScopeMap { get; init; }

    public required OrderContext OrderContext { get; init; }
}