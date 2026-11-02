using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using QPS.Shared.Common;

namespace QPS.Host.Filters;

/// <summary>
/// 统一响应包装过滤器，保证每个响应只被包装一次。
/// </summary>
public class ResponseWrapperFilter : IAsyncResultFilter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = global::System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        switch (context.Result)
        {
            case ObjectResult objectResult:
            {
                var value = objectResult.Value;
                var valueType = value?.GetType();
                if (valueType is { IsGenericType: true } &&
                    valueType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
                {
                    context.Result = new JsonResult(value, JsonOptions) { StatusCode = 200 };
                    await next();
                    return;
                }

                var statusCode = objectResult.StatusCode;
                if (statusCode.HasValue && statusCode.Value != 200)
                {
                    await next();
                    return;
                }

                context.Result = new JsonResult(ApiResponse<object>.Success(value), JsonOptions) { StatusCode = 200 };
                await next();
                return;
            }
            case StatusCodeResult statusCodeResult:
                if (statusCodeResult.StatusCode != 200)
                {
                    await next();
                    return;
                }
                break;
        }

        object? payload = context.Result is ObjectResult r ? r.Value : null;
        context.Result = new JsonResult(ApiResponse<object>.Success(payload), JsonOptions) { StatusCode = 200 };
        await next();
    }
}