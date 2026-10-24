using Microsoft.Extensions.Logging;
using QPS.System.Contracts;
using QPS.System.Domain.Entities;

namespace QPS.System.Application.Services;

public class ErrorLogService : IErrorLogService
{
    private readonly ISystemDbContext _dbContext;
    private readonly ILogger<ErrorLogService> _logger;

    public ErrorLogService(ISystemDbContext dbContext, ILogger<ErrorLogService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogErrorAsync(
        Exception exception,
        string requestUrl,
        string requestMethod,
        string requestBody,
        string userId,
        string username,
        string ipAddress,
        string userAgent,
        int httpStatusCode)
    {
        try
        {
            var errorLog = new SystemErrorLog(
                exception.GetType().FullName ?? "Unknown",
                exception.Message,
                exception.StackTrace ?? string.Empty,
                requestUrl,
                requestMethod,
                requestBody,
                userId,
                username,
                ipAddress,
                userAgent,
                httpStatusCode);

            await _dbContext.SystemErrorLogs.AddAsync(errorLog);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception logException)
        {
            _logger.LogError(logException, "写入错误日志失败");
        }
    }
}
