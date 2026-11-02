using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallStocks;

/// <summary>
/// 批量调整规格价格与库存。
/// </summary>
public class BatchAdjustMallSkusCommand : IRequest<bool>
{
    public MallSkuBatchAdjustRequest Request { get; set; } = null!;
}

public class BatchAdjustMallSkusHandler : IRequestHandler<BatchAdjustMallSkusCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public BatchAdjustMallSkusHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(BatchAdjustMallSkusCommand request, CancellationToken cancellationToken)
    {
        var ids = (request.Request.Ids ?? new List<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            throw new BusinessException(400, "请选择要调整的规格");
        }

        if (!request.Request.Price.HasValue && !request.Request.TargetStock.HasValue)
        {
            throw new BusinessException(400, "请至少填写价格或目标库存");
        }

        if (request.Request.Price.HasValue)
        {
            MallStockRules.ValidatePrice(request.Request.Price.Value);
        }

        if (request.Request.TargetStock.HasValue)
        {
            MallStockRules.ValidateTargetStock(request.Request.TargetStock.Value);
        }

        var remark = MallStockRules.NormalizeRemark(request.Request.Remark);

        var skus = await _dbContext.MallSkus
            .Where(sku => ids.Contains(sku.Id) && !sku.IsDeleted)
            .ToListAsync(cancellationToken);

        if (skus.Count != ids.Count)
        {
            throw new BusinessException(404, "部分规格不存在");
        }

        foreach (var sku in skus)
        {
            if (request.Request.Price.HasValue)
            {
                sku.ChangePrice(request.Request.Price.Value);
            }

            if (request.Request.TargetStock.HasValue && sku.Stock != request.Request.TargetStock.Value)
            {
                var beforeStock = sku.AdjustStock(request.Request.TargetStock.Value);

                _dbContext.MallStockLogs.Add(MallStockLog.Create(
                    sku.Id,
                    sku.SkuCode,
                    sku.SpecName,
                    MallStockChangeType.ManualAdjust,
                    beforeStock,
                    sku.Stock,
                    remark: remark ?? "批量调整库存"));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
