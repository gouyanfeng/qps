using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;
using QPS.Shared.Extensions;

namespace QPS.Mall.Application.Features.MallStocks;

/// <summary>
/// 分页查询库存变更流水。
/// </summary>
public class GetMallStockLogsQuery : PaginationRequest, IRequest<PaginationResponse<MallStockLogDto>>
{
    /// <summary>
    /// 按规格过滤。
    /// </summary>
    public Guid? SkuId { get; set; }

    /// <summary>
    /// 按变更类型过滤。
    /// </summary>
    public string? ChangeType { get; set; }

    /// <summary>
    /// 变更时间起。
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 变更时间止。
    /// </summary>
    public DateTime? EndTime { get; set; }
}

public class GetMallStockLogsHandler : IRequestHandler<GetMallStockLogsQuery, PaginationResponse<MallStockLogDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallStockLogsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallStockLogDto>> Handle(
        GetMallStockLogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallStockLogs
            .Where(log => !log.IsDeleted)
            .AsQueryable();

        if (request.SkuId.HasValue)
        {
            query = query.Where(log => log.SkuId == request.SkuId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ChangeType))
        {
            var changeType = request.ChangeType.Trim();
            query = query.Where(log => log.ChangeType == changeType);
        }

        if (request.StartTime.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= request.StartTime.Value);
        }

        if (request.EndTime.HasValue)
        {
            query = query.Where(log => log.CreatedAt <= request.EndTime.Value);
        }

        var dtoQuery = query.Select(log => new MallStockLogDto
        {
            Id = log.Id,
            SkuId = log.SkuId,
            SkuCode = log.SkuCode,
            SpecName = log.SpecName,
            ChangeType = log.ChangeType,
            ChangeQuantity = log.ChangeQuantity,
            BeforeStock = log.BeforeStock,
            AfterStock = log.AfterStock,
            RelatedOrderNo = log.RelatedOrderNo,
            Remark = log.Remark,
            OperatorName = log.CreatedBy,
            CreatedAt = log.CreatedAt
        });

        return await dtoQuery.ToPaginationResponseAsync(request, cancellationToken);
    }
}
