using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QPS.Host.Controllers;

[ApiController]
[Route("")]
[AllowAnonymous]
public class LandingController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public LandingController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("/")]
    [HttpGet("/api")]
    public IActionResult Index()
    {
        var serviceAssembly = typeof(LandingController).Assembly;
        var serviceVersion = serviceAssembly.GetName().Version?.ToString() ?? "unknown";
        var buildTimeUtc = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var environmentName = _environment.EnvironmentName
            .Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;").Replace("\"", "&quot;");

        var html = $$"""
            <!DOCTYPE html>
            <html lang="zh-CN">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>QPS API</title>
            <style>
            body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;background:#0f172a;color:#e2e8f0;font-family:system-ui,-apple-system,sans-serif}
            main{max-width:520px;padding:32px}
            h1{margin:0 0 8px;font-size:22px}
            p{margin:0 0 20px;color:#94a3b8}
            dl{display:grid;grid-template-columns:auto 1fr;gap:8px 16px;margin:0}
            dt{color:#94a3b8}
            dd{margin:0;font-variant-numeric:tabular-nums}
            </style>
            </head>
            <body>
            <main>
            <h1>QPS API</h1>
            <p>服务正在运行。</p>
            <dl>
            <dt>环境</dt><dd>{{environmentName}}</dd>
            <dt>版本</dt><dd>{{serviceVersion}}</dd>
            <dt>构建时间</dt><dd>{{buildTimeUtc}}</dd>
            </dl>
            </main>
            </body>
            </html>
            """;

        return Content(html, "text/html; charset=utf-8");
    }
}