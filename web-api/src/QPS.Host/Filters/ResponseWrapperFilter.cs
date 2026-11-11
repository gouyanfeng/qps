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
            // 落地页等原生 HTML 响应原样放行，不包 ApiResponse
            case ContentResult contentResult when
                contentResult.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true:
                await next();
                return;
            // 文件下载（FileContentResult/FileStreamResult/PhysicalFileResult 等）原样放行
            case FileResult:
                await next();
                return;
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

                // value 为 null 时仍按成功返回，data 置空即可，故此处用 ! 抑制可空告警。
                context.Result = new JsonResult(ApiResponse<object>.Success(value!), JsonOptions) { StatusCode = 200 };
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
        context.Result = new JsonResult(ApiResponse<object>.Success(payload!), JsonOptions) { StatusCode = 200 };
        await next();
    }
}