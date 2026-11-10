using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using QPS.Crm;
using QPS.Host.Middleware;
using QPS.Host.Registration;
using QPS.Mall;
using QPS.Persistence;
using QPS.Persistence.Database;
using QPS.Persistence.Seeds;
using QPS.Shared;
using QPS.Shared.Common;
using QPS.System;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

#region 服务注册

// HTTP 基础设施：MVC 与响应包装、Controller 发现范围、CORS、JWT 鉴权；Swagger 仅开发环境注册。
var mvcBuilder = builder.Services.AddQpsMvc();
mvcBuilder.ConfigureQpsApplicationParts(typeof(Program).Assembly);

builder.Services.AddQpsCors();

// Swagger 只在 Development 注册：生产不生成文档、不挂 UI，避免暴露接口清单。
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddQpsSwagger();
}

// JWT 配置只读取一次，令牌校验（Host）与令牌签发（Persistence）共用同一实例。
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("缺少 Jwt 配置节点。");
if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
{
    throw new InvalidOperationException("Jwt 配置不完整，请检查 SecretKey。");
}
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddQpsJwtAuthentication(jwtOptions);

builder.Services.AddHttpContextAccessor();

// 公共能力：契约、Pipeline、领域事件
builder.Services.AddQpsShared();

// 持久化基础设施为必需项
builder.Services.AddQpsPersistence(builder.Configuration);

// 模块注册：调用即启用，不调用则其 Handler / Controller 不参与运行
builder.Services.AddQpsSystem(mvcBuilder);
builder.Services.AddQpsCrm(mvcBuilder);
builder.Services.AddQpsMall(mvcBuilder);

#endregion

var app = builder.Build();

#region 数据库初始化

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var connStringForDiagnostic = dbContext.Database.GetConnectionString();
var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");

if (!await dbContext.Database.CanConnectAsync())
{
    Console.Error.WriteLine("[FATAL] 数据库连接失败，连接串: {0}", connStringForDiagnostic ?? "无法获取");
    throw new InvalidOperationException("数据库无法连接。");
}

if (app.Environment.IsDevelopment())
{
    var strategy = dbContext.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () =>
    {
        await dbContext.Database.MigrateAsync();
    });

    var workspaceDirectory = new DirectoryInfo(app.Environment.ContentRootPath);
    while (workspaceDirectory is not null &&
           !Directory.Exists(Path.Combine(workspaceDirectory.FullName, "mobile", "src", "mock")))
    {
        workspaceDirectory = workspaceDirectory.Parent;
    }

    var mockPath = workspaceDirectory is null
        ? string.Empty
        : Path.Combine(workspaceDirectory.FullName, "mobile", "src", "mock");
    if (Directory.Exists(mockPath))
    {
        await MallMockDataSeeder.SeedAsync(dbContext, logger, mockPath);
    }
    else
    {
        logger.LogWarning("⚠️  Mock 数据目录不存在: {Path}", mockPath);
    }
}

#endregion

#region HTTP请求管道配置

// 与注册保持同一条件：只有开发环境才挂 Swagger 中间件。
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// 上传目录在启动时确保存在，避免 wwwroot 缺失导致静态文件提供器为空。
var webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath),
    OnPrepareResponse = context =>
    {
        // 上传目录内文件名均为 GUID，内容不会覆盖，可交由浏览器长期缓存。
        if (context.Context.Request.Path.StartsWithSegments("/uploads"))
        {
            context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        }
    }
});

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 匿名落地页：只暴露服务名、环境、版本与构建时间，显式 AllowAnonymous 以绕开 FallbackPolicy。
var serviceAssembly = typeof(Program).Assembly;
var serviceVersion = serviceAssembly.GetName().Version?.ToString() ?? "unknown";
var buildTimeUtc =DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
var environmentName = System.Net.WebUtility.HtmlEncode(app.Environment.EnvironmentName);

Func<string> buildLandingPage = () => $$"""
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

app.MapGet("/", () => Results.Content(buildLandingPage(), "text/html; charset=utf-8")).AllowAnonymous();
app.MapGet("/api", () => Results.Content(buildLandingPage(), "text/html; charset=utf-8")).AllowAnonymous();

app.MapFallback(() => Results.NotFound()).AllowAnonymous();

#endregion

app.Run();