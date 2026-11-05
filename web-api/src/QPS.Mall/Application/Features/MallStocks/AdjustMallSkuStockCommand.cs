using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallStocks;

/// <summary>
/// 手动调整规格库存。
/// </summary>
public class AdjustMallSkuStockCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallSkuAdjustStockRequest Request { get; set; } = null!;
}

public class AdjustMallSkuStockHandler : IRequestHandler<AdjustMallSkuStockCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public AdjustMallSkuStockHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(AdjustMallSkuStockCommand request, CancellationToken cancellationToken)
    {
        var sku = await _dbContext.MallSkus
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (sku is null)
        {
            throw new BusinessException(404, "规格不存在");
        }

        MallStockRules.ValidateTargetStock(request.Request.TargetStock);
        var remark = MallStockRules.NormalizeRemark(request.Request.Remark);

        if (sku.Stock == request.Request.TargetStock)
        {
            return true;
        }

        var beforeStock = sku.AdjustStock(request.Request.TargetStock);

        _dbContext.MallStockLogs.Add(MallStockLog.Create(
            sku.Id,
            sku.SkuCode,
            sku.SpecName,
            MallStockChangeType.ManualAdjust,
            beforeStock,
            sku.Stock,
            remark: remark));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
