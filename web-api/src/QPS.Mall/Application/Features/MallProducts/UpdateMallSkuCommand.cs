using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 编辑商品规格。
/// </summary>
public class UpdateMallSkuCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallSkuUpdateRequest Request { get; set; } = null!;
}

public class UpdateMallSkuHandler : IRequestHandler<UpdateMallSkuCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public UpdateMallSkuHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateMallSkuCommand request, CancellationToken cancellationToken)
    {
        var sku = await _dbContext.MallSkus
            .FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, cancellationToken);

        if (sku is null)
        {
            throw new BusinessException(404, "规格不存在");
        }

        var skuCode = MallProductRules.NormalizeSkuCode(request.Request.SkuCode);
        var specName = MallProductRules.NormalizeSpecName(request.Request.SpecName);
        MallProductRules.ValidatePrice(request.Request.Price);
        MallProductRules.ValidateMarketPrice(request.Request.MarketPrice);
        MallProductRules.ValidateWeight(request.Request.WeightGram);
        MallProductRules.ValidateStock(request.Request.Stock);
        MallProductRules.ValidateWarnStock(request.Request.WarnStock);

        await MallProductRules.EnsureSkuCodeNotDuplicatedAsync(_dbContext, skuCode, request.Id, cancellationToken);

        var beforeStock = sku.Stock;

        sku.Update(
            skuCode,
            specName,
            request.Request.Price,
            request.Request.WeightGram,
            request.Request.Stock,
            request.Request.WarnStock,
            marketPrice: request.Request.MarketPrice);

        if (beforeStock != sku.Stock)
        {
            _dbContext.MallStockLogs.Add(MallStockLog.Create(
                sku.Id,
                sku.SkuCode,
                sku.SpecName,
                MallStockChangeType.ManualAdjust,
                beforeStock,
                sku.Stock,
                remark: "编辑规格同步修改库存"));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
