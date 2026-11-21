using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QPS.System.Contracts;
using QPS.System.Domain.Entities;

namespace QPS.System.Application.Services;

public class ErrorLogService : IErrorLogService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErrorLogService> _logger;

    public ErrorLogService(
        IServiceScopeFactory scopeFactory,
        ILogger<ErrorLogService> logger)
    {
        _scopeFactory = scopeFactory;
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
            // Resolve DbContext from a brand-new DI scope so we get a fresh
            // instance with a clean ChangeTracker — isolated from any dirty
            // entities left over by the failing request path.
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ISystemDbContext>();

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

            await dbContext.SystemErrorLogs.AddAsync(errorLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception logException)
        {
            _logger.LogError(logException, "写入错误日志失败");
        }
    }
}
