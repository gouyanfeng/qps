using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallFreightRules;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.Shared.Exceptions;

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
    private readonly MallPromotionContextBuilder _promotionContextBuilder;
    private readonly MallCouponService _couponService;

    public CreateMallOrderHandler(
        IMallDbContext dbContext,
        MallPromotionEngine promotionEngine,
        MallPromotionContextBuilder promotionContextBuilder,
        MallCouponService couponService)
    {
        _dbContext = dbContext;
        _promotionEngine = promotionEngine;
        _promotionContextBuilder = promotionContextBuilder;
        _couponService = couponService;
    }

    public async Task<MallOrderDetailDto> Handle(
        CreateMallOrderCommand command,
        CancellationToken cancellationToken)
    {
        // ===== 阶段一：装配 —— 登录与地址快照、入参校验、SKU 与库存预检、促销数据装配 =====
        var context = await BuildOrderContextAsync(command.Request, cancellationToken);

        // ===== 阶段二：促销计价 —— 引擎匹配有效促销并逐行应用（改价/赠品/免运费标记），计算原价与促销优惠 =====
        var applyResult = ApplyPromotions(context);
        var goodsAmount = CalculateOriginalGoodsAmount(applyResult);
        var promotionDiscountAmount = Math.Max(
            0,
            goodsAmount - CalculatePromotedGoodsAmount(applyResult));

        // ===== 阶段二之二：优惠券抵扣 —— 先商品金额、再促销、后券，券按促销后金额校验门槛并抵扣 =====
        var couponDiscountAmount = 0m;
        Guid? couponId = null;
        var couponName = string.Empty;
        if (command.Request.MemberCouponId.HasValue)
        {
            var evaluation = await _couponService.EvaluateAsync(
                command.Request.MemberId!.Value,
                command.Request.MemberCouponId.Value,
                applyResult.Items,
                cancellationToken);

            couponDiscountAmount = evaluation.DiscountAmount;
            couponId = evaluation.CouponId;
            couponName = evaluation.CouponName;
        }

        // ===== 阶段三：运费 —— 按商品运费模板（回退默认模板）与收货区域计算，促销免运费时清零 =====
        var freightAmount = await CalculateFreightAsync(context, applyResult, cancellationToken);

        // ===== 阶段四：落库 —— 订单、明细（正常/赠品）、库存扣减与流水 =====
        var order = await CreateOrderAsync(
            context,
            applyResult,
            goodsAmount,
            promotionDiscountAmount,
            couponDiscountAmount,
            couponId,
            couponName,
            freightAmount,
            cancellationToken);

        // 券核销与订单写入同事务：条件更新失败（已使用/已过期）时整体回滚
        if (command.Request.MemberCouponId.HasValue)
        {
            await MallCouponService.RedeemAsync(
                _dbContext,
                command.Request.MemberCouponId.Value,
                command.Request.MemberId!.Value,
                order.OrderNo,
                cancellationToken);
        }

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
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new BusinessException(400, "请至少提交一个商品规格");
        }

        var address = await ResolveReceiverAddressAsync(request, cancellationToken);

        // DataAnnotations 已覆盖备注长度、明细非空与数量范围；此处仅保留跨行规则
        if (request.Items.Select(item => item.SkuId).Distinct().Count() != request.Items.Count)
        {
            throw new BusinessException(400, "同一规格不能重复提交");
        }

        var skuMap = await LoadAndValidateSkuMapAsync(request.Items, cancellationToken);
        var totals = SumOrderTotals(request.Items, skuMap);
        var quantitiesBySku = request.Items
            .GroupBy(item => item.SkuId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var promotionData = await _promotionContextBuilder.BuildAsync(
            request.MemberId!.Value,
            skuMap.Values
                .Select(row => new MallPromotionSkuInput(
                    row.Sku,
                    row.Product,
                    quantitiesBySku[row.Sku.Id]))
                .ToList(),
            cancellationToken);

        return new MallOrderBuildContext
        {
            Request = request,
            Address = address,
            SkuMap = skuMap,
            Totals = totals,
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
            .FirstOrDefaultAsync(item => item.Id == request.AddressId, cancellationToken);
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
                where ids.Contains(sku.Id)
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

    /// <summary>阶段二：促销引擎匹配与应用（逐行改价、免运费标记、赠品清单）。</summary>
    private ApplyResult ApplyPromotions(MallOrderBuildContext context)
    {
        var orderCtx = context.PromotionOrderContext!;
        var matchedPromotions = _promotionEngine.Match(context.ActivePromotions, context.PromotionScopeMap, orderCtx);
        return _promotionEngine.Apply(orderCtx.Items, matchedPromotions);
    }

    /// <summary>商品原价合计 = 各行促销前单价 × 数量。</summary>
    private static decimal CalculateOriginalGoodsAmount(ApplyResult applyResult)
    {
        return applyResult.Items.Sum(i => i.OriginalPrice * i.Quantity);
    }

    /// <summary>促销后商品金额 = 各行改价后单价 × 数量。</summary>
    private static decimal CalculatePromotedGoodsAmount(ApplyResult applyResult)
    {
        return applyResult.Items.Sum(i => i.CurrentPrice * i.Quantity);
    }

    /// <summary>阶段三：加载运费模板与区域并计算运费；促销免运费时清零。</summary>
    private async Task<decimal> CalculateFreightAsync(
        MallOrderBuildContext context,
        ApplyResult applyResult,
        CancellationToken cancellationToken)
    {
        var freightItems = BuildFreightItems(context, applyResult);

        var addressInfo = new FreightAddressInfo
        {
            ProvinceName = context.Address.ProvinceName,
            CityName = context.Address.CityName
        };

        // 与预览共用同一运费口径：模板缺失/停用/无区域时返回 0，不阻断下单
        return await MallFreightCalculator.CalculateAsync(
            _dbContext,
            addressInfo,
            freightItems,
            applyResult.IsFreightFree,
            cancellationToken);
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

    /// <summary>阶段四：生成订单与明细（正常行、赠品行），扣减库存并写流水（随 SaveChanges 一次提交）。</summary>
    private async Task<MallOrder> CreateOrderAsync(
        MallOrderBuildContext context,
        ApplyResult applyResult,
        decimal goodsAmount,
        decimal promotionDiscountAmount,
        decimal couponDiscountAmount,
        Guid? couponId,
        string couponName,
        decimal freightAmount,
        CancellationToken cancellationToken)
    {
        var request = context.Request;
        var orderNo = await GenerateOrderNoAsync(cancellationToken);
        var fullReceiverAddress = context.ResolveFullReceiverAddress();

        // 快照字段统一由 OrderSnapshotBuilder 写入，订单流程只负责提供业务数据
        var order = OrderSnapshotBuilder.BuildOrder(new OrderHeaderSnapshot
        {
            OrderNo = orderNo,
            MemberId = request.MemberId,
            ReceiverName = context.Address.ReceiverName,
            ReceiverPhone = context.Address.ReceiverPhone,
            ReceiverAddress = fullReceiverAddress,
            GoodsAmount = goodsAmount,
            PromotionDiscountAmount = promotionDiscountAmount,
            CouponDiscountAmount = couponDiscountAmount,
            CouponId = couponId,
            CouponName = couponName,
            FreightAmount = freightAmount,
            TotalWeightGram = context.Totals.TotalWeightGram,
            Remark = request.Remark?.Trim() ?? string.Empty
        });

        _dbContext.MallOrders.Add(order);

        var deductions = new List<(MallSku Sku, int Quantity)>();
        var orderItems = new List<MallOrderItem>();
        AppendProductLines(order, context, applyResult, deductions, orderItems);
        await AppendGiftLinesAsync(order, applyResult, orderItems, cancellationToken);
        OrderSnapshotBuilder.EnsureLinePromotionTotal(order, orderItems);
        _dbContext.MallOrderItems.AddRange(orderItems);

        await ApplyStockDeductionsAsync(order, deductions, cancellationToken);

        return order;
    }

    /// <summary>阶段四：生成订单号并确保唯一。</summary>
    private async Task<string> GenerateOrderNoAsync(CancellationToken cancellationToken)
    {
        var orderNo = MallNumberGenerator.Generate("ORD");

        var orderNoExists = await _dbContext.MallOrders
            .AnyAsync(order => order.OrderNo == orderNo, cancellationToken);
        if (orderNoExists)
        {
            throw new BusinessException(400, "订单号已存在");
        }

        return orderNo;
    }

    /// <summary>阶段四：正常商品行 —— 记录库存扣减与订单明细（命中促销时记录促销来源与原价）。</summary>
    private static void AppendProductLines(
        MallOrder order,
        MallOrderBuildContext context,
        ApplyResult applyResult,
        List<(MallSku Sku, int Quantity)> deductions,
        List<MallOrderItem> orderItems)
    {
        foreach (var appliedItem in applyResult.Items)
        {
            var row = context.SkuMap[appliedItem.SkuId];
            deductions.Add((row.Sku, appliedItem.Quantity));
            orderItems.Add(OrderSnapshotBuilder.BuildProductItem(order, row.Sku, row.Product, appliedItem));
        }
    }

    /// <summary>
    /// 赠品按 SKU 条件更新原子扣库存；库存不足或规格失效时不发放该赠品、记录原因，不拦截下单。
    /// </summary>
    private async Task AppendGiftLinesAsync(
        MallOrder order,
        ApplyResult applyResult,
        List<MallOrderItem> orderItems,
        CancellationToken cancellationToken)
    {
        if (applyResult.GiftSkus.Count == 0)
        {
            return;
        }

        var giftSkuMap = await LoadSkuProductMapAsync(
            applyResult.GiftSkus.Select(g => g.SkuId).ToList(), cancellationToken);
        var shortages = new List<string>();

        foreach (var gift in applyResult.GiftSkus)
        {
            var giftRow = giftSkuMap.GetValueOrDefault(gift.SkuId);
            if (giftRow.Sku is null || !giftRow.Sku.IsActive || !giftRow.Product.IsActive)
            {
                shortages.Add($"赠品「{gift.PromotionName}」规格不存在或已下架，未发放");
                continue;
            }

            // 条件更新：库存不足时受影响行数为 0，仅跳过该赠品，不回滚订单
            var affected = await _dbContext.MallSkus
                .Where(sku => sku.Id == gift.SkuId && sku.Stock >= gift.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        sku => sku.Stock,
                        sku => sku.Stock - gift.Quantity),
                    cancellationToken);

            if (affected == 0)
            {
                shortages.Add($"赠品「{giftRow.Product.Name}·{giftRow.Sku.SpecName}」库存不足，未发放");
                continue;
            }

            // 回查扣减后的真实库存写流水，避免使用内存旧值
            var finalStock = await _dbContext.MallSkus
                .AsNoTracking()
                .Where(sku => sku.Id == gift.SkuId)
                .Select(sku => (int?)sku.Stock)
                .FirstOrDefaultAsync(cancellationToken) ?? 0;

            _dbContext.MallStockLogs.Add(MallStockLog.Create(
                giftRow.Sku.Id,
                giftRow.Sku.SkuCode,
                giftRow.Sku.SpecName,
                MallStockChangeType.OrderDeduct,
                finalStock + gift.Quantity,
                finalStock,
                relatedOrderNo: order.OrderNo));

            orderItems.Add(OrderSnapshotBuilder.BuildGiftItem(
                order, giftRow.Sku, giftRow.Product, gift));
        }

        if (shortages.Count > 0)
        {
            order.RecordGiftShortage(string.Join("；", shortages));
        }
    }

    /// <summary>
    /// 调用前先按库存预检给出友好错误，最终以条件更新的受影响行数为准，避免校验与扣减之间出现窗口。
    /// </summary>
    private async Task ApplyStockDeductionsAsync(
        MallOrder order,
        List<(MallSku Sku, int Quantity)> deductions,
        CancellationToken cancellationToken)
    {
        if (deductions.Count == 0)
        {
            return;
        }

        // 按 SKU 汇总并按 Id 升序更新，降低并发下单时死锁概率
        var skuGroups = deductions
            .GroupBy(deduction => deduction.Sku.Id)
            .Select(group => new
            {
                SkuId = group.Key,
                Quantity = group.Sum(deduction => deduction.Quantity),
                Sku = group.First().Sku
            })
            .OrderBy(group => group.SkuId)
            .ToList();

        foreach (var group in skuGroups)
        {
            // 条件更新：库存不足时受影响行数为 0，抛业务异常并回滚整个下单事务
            var affected = await _dbContext.MallSkus
                .Where(sku => sku.Id == group.SkuId && sku.Stock >= group.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        sku => sku.Stock,
                        sku => sku.Stock - group.Quantity),
                    cancellationToken);

            if (affected == 0)
            {
                throw new BusinessException(400, $"规格「{group.Sku.SpecName}」库存不足");
            }
        }

        // ExecuteUpdateAsync 绕过变更跟踪，回查数据库真实库存后再写流水，避免使用内存旧值
        var skuIds = skuGroups.Select(group => group.SkuId).ToList();
        var stockById = await _dbContext.MallSkus
            .AsNoTracking()
            .Where(sku => skuIds.Contains(sku.Id))
            .Select(sku => new { sku.Id, sku.Stock })
            .ToDictionaryAsync(sku => sku.Id, sku => sku.Stock, cancellationToken);

        foreach (var group in skuGroups)
        {
            var finalStock = stockById.TryGetValue(group.SkuId, out var currentStock)
                ? currentStock
                : group.Sku.Stock - group.Quantity;
            var groupDeductions = deductions
                .Where(deduction => deduction.Sku.Id == group.SkuId)
                .ToList();

            // 由扣减后的真实库存倒推每行流水的前后值
            var runningAfter = finalStock;
            for (var index = groupDeductions.Count - 1; index >= 0; index--)
            {
                var quantity = groupDeductions[index].Quantity;
                var afterStock = runningAfter;
                var beforeStock = afterStock + quantity;
                runningAfter = beforeStock;

                _dbContext.MallStockLogs.Add(MallStockLog.Create(
                    group.Sku.Id,
                    group.Sku.SkuCode,
                    group.Sku.SpecName,
                    MallStockChangeType.OrderDeduct,
                    beforeStock,
                    afterStock,
                    relatedOrderNo: order.OrderNo));
            }
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

