using QPS.Shared.Common;
using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.OperationLogs;
using QPS.Shared.Extensions;
using QPS.Shared.Contracts;

namespace QPS.System.Application.Features.OperationLogs;

public class GetOperationLogsQuery : PaginationRequest, IRequest<PaginationResponse<OperationLogDto>>
{
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? ActionType { get; set; }
    public string? OperatorName { get; set; }
    public string? RequestPath { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
}

public class GetOperationLogsQueryHandler : IRequestHandler<GetOperationLogsQuery, PaginationResponse<OperationLogDto>>
{
    private readonly ISystemDbContext _dbContext;

    public GetOperationLogsQueryHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<OperationLogDto>> Handle(
        GetOperationLogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.SystemOperationLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(log => log.EntityType.Contains(request.EntityType));
        }

        if (!string.IsNullOrWhiteSpace(request.EntityId))
        {
            query = query.Where(log => log.EntityId.Contains(request.EntityId));
        }

        if (!string.IsNullOrWhiteSpace(request.ActionType))
        {
            query = query.Where(log => log.ActionType == request.ActionType);
        }

        if (!string.IsNullOrWhiteSpace(request.OperatorName))
        {
            query = query.Where(log => log.OperatorName.Contains(request.OperatorName));
        }

        if (!string.IsNullOrWhiteSpace(request.RequestPath))
        {
            query = query.Where(log => log.RequestPath.Contains(request.RequestPath));
        }

        if (request.StartAt.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= request.StartAt.Value);
        }

        if (request.EndAt.HasValue)
        {
            query = query.Where(log => log.CreatedAt <= request.EndAt.Value);
        }

        var logs = query
            .Select(log => new OperationLogDto
            {
                Id = log.Id,
                CreatedAt = log.CreatedAt,
                ActionType = log.ActionType,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                OperatorName = log.OperatorName,
                RequestPath = log.RequestPath,
                IpAddress = log.IpAddress,
                ChangeJson = log.ChangeJson
            });

        return await logs.ToPaginationResponseAsync(request);
    }
}
