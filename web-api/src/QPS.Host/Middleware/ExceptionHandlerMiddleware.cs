using System.Text;
using System.Text.Json;
using QPS.Shared.Exceptions;
using QPS.System.Contracts;

namespace QPS.Host.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = global::System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IErrorLogService errorLogService)
    {
        try
        {
            context.Request.EnableBuffering();
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception, errorLogService);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        IErrorLogService errorLogService)
    {
        try
        {
            var requestUrl = context.Request.Path + context.Request.QueryString;
            var requestMethod = context.Request.Method;
            var requestBody = string.Empty;

            try
            {
                if (context.Request.Body.CanSeek)
                {
                    context.Request.Body.Position = 0;
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                    requestBody = await reader.ReadToEndAsync();
                }
            }
            catch (Exception bodyException)
            {
                _logger.LogWarning(bodyException, "读取异常请求体失败");
            }

            var userId = context.User.FindFirst("userId")?.Value ?? string.Empty;
            var username = context.User.FindFirst("username")?.Value ?? string.Empty;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var userAgent = context.Request.Headers.UserAgent.ToString();

            var httpStatusCode = exception is BusinessException businessException
                ? businessException.ErrorCode
                : 500;

            await errorLogService.LogErrorAsync(
                exception,
                requestUrl,
                requestMethod,
                requestBody,
                userId,
                username,
                ipAddress,
                userAgent,
                httpStatusCode);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";

            var response = QPS.Shared.Common.ApiResponse<object>.Fail(httpStatusCode, exception.Message);
            await context.Response.WriteAsJsonAsync(response, JsonOptions);
        }
        catch (Exception handlerException)
        {
            _logger.LogError(handlerException, "全局异常处理失败");
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"code\":500,\"msg\":\"Critical error\"}");
        }
    }
}
