using QPS.Shared.Common;
using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.ErrorLogs;
using QPS.Shared.Extensions;
using QPS.Shared.Contracts;

namespace QPS.System.Application.Features.ErrorLogs;

public class GetErrorLogsQuery : PaginationRequest, IRequest<PaginationResponse<ErrorLogDto>>
{
    public string? ErrorType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RequestUrl { get; set; }
    public string? Username { get; set; }
    public int? HttpStatusCode { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
}

public class GetErrorLogsQueryHandler : IRequestHandler<GetErrorLogsQuery, PaginationResponse<ErrorLogDto>>
{
    private readonly ISystemDbContext _dbContext;

    public GetErrorLogsQueryHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<ErrorLogDto>> Handle(GetErrorLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.SystemErrorLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ErrorType))
        {
            query = query.Where(log => log.ErrorType.Contains(request.ErrorType));
        }

        if (!string.IsNullOrWhiteSpace(request.ErrorMessage))
        {
            query = query.Where(log => log.ErrorMessage.Contains(request.ErrorMessage));
        }

        if (!string.IsNullOrWhiteSpace(request.RequestUrl))
        {
            query = query.Where(log => log.RequestUrl.Contains(request.RequestUrl));
        }

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            query = query.Where(log => log.Username.Contains(request.Username));
        }

        if (request.HttpStatusCode.HasValue)
        {
            query = query.Where(log => log.HttpStatusCode == request.HttpStatusCode.Value);
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
            .Select(log => new ErrorLogDto
            {
                Id = log.Id,
                CreatedAt = log.CreatedAt,
                ErrorType = log.ErrorType,
                ErrorMessage = log.ErrorMessage,
                StackTrace = log.StackTrace,
                RequestUrl = log.RequestUrl,
                RequestMethod = log.RequestMethod,
                RequestBody = log.RequestBody,
                UserId = log.UserId,
                Username = log.Username,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                HttpStatusCode = log.HttpStatusCode
            });

        return await logs.ToPaginationResponseAsync(request);
    }
}
