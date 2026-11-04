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

// HTTP 基础设施：MVC 与响应包装、Controller 发现范围、CORS、Swagger、JWT 鉴权。
var mvcBuilder = builder.Services.AddQpsMvc();
mvcBuilder.ConfigureQpsApplicationParts(typeof(Program).Assembly);

builder.Services.AddQpsCors();
builder.Services.AddQpsSwagger();

// JWT 配置只读取一次，令牌校验（Host）与令牌签发（Persistence）共用同一实例。
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("缺少 Jwt 配置节点。");
if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) ||
    string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException("Jwt 配置不完整，请检查 SecretKey、Issuer、Audience。");
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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");

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
    else
    {
        await dbContext.Database.CanConnectAsync();
    }
}

#endregion

#region HTTP请求管道配置

app.UseSwagger();
app.UseSwaggerUI();

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

#endregion

app.Run();