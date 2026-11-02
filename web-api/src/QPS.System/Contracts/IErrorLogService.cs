namespace QPS.System.Contracts;

public interface IErrorLogService
{
    Task LogErrorAsync(
        Exception exception,
        string requestUrl,
        string requestMethod,
        string requestBody,
        string userId,
        string username,
        string ipAddress,
        string userAgent,
        int httpStatusCode);
}
