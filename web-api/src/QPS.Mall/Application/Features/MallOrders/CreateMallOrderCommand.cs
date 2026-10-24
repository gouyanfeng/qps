using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallFreightRules;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 外部下单：校验库存、扣减库存、计算运费并生成订单。
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

    public CreateMallOrderHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallOrderDetailDto> Handle(
        CreateMallOrderCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (request.StoreId == Guid.Empty)
        {
            throw new BusinessException(400, "请选择下单门店");
        }

        var receiverName = NormalizeRequired(request.ReceiverName, ReceiverNameMaxLength, "收货人姓名");
        var receiverPhone = NormalizeRequired(request.ReceiverPhone, ReceiverPhoneMaxLength, "收货人电话");
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

        var store = await _dbContext.MallStores
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.StoreId && !item.IsDeleted, cancellationToken);

        if (store is null)
        {
            throw new BusinessException(404, "门店不存在");
        }

        if (!store.IsActive)
        {
            throw new BusinessException(400, "门店已停用，无法下单");
        }

        var freightRule = await _dbContext.MallFreightRules
            .AsNoTracking()
            .FirstOrDefaultAsync(
                rule => rule.StoreId == request.StoreId && rule.IsActive && !rule.IsDeleted,
                cancellationToken);

        if (freightRule is null)
        {
            throw new BusinessException(400, "门店未配置运费规则，无法下单");
        }

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

        decimal goodsAmount = 0;
        var totalWeightGram = 0;

        foreach (var item in request.Items)
        {
            var row = skuMap[item.SkuId];
            goodsAmount += row.Sku.Price * item.Quantity;
            totalWeightGram += row.Sku.WeightGram * item.Quantity;
        }

        var freightAmount = MallFreightCalculator.Calculate(totalWeightGram, goodsAmount, freightRule);

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
            store.Id,
            receiverName,
            receiverPhone,
            receiverAddress,
            goodsAmount,
            freightAmount,
            totalWeightGram,
            remark);

        _dbContext.MallOrders.Add(order);

        foreach (var item in request.Items)
        {
            var row = skuMap[item.SkuId];
            var sku = row.Sku;
            var beforeStock = sku.DeductStock(item.Quantity);

            _dbContext.MallStockLogs.Add(MallStockLog.Create(
                sku.Id,
                sku.SkuCode,
                sku.SpecName,
                MallStockChangeType.OrderDeduct,
                beforeStock,
                sku.Stock,
                relatedOrderNo: orderNo));

            _dbContext.MallOrderItems.Add(MallOrderItem.Create(
                order.Id,
                row.Sku.Id,
                row.Product.Id,
                row.Product.Name,
                row.Sku.SkuCode,
                row.Sku.SpecName,
                row.Sku.Price,
                item.Quantity,
                row.Sku.WeightGram));
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
