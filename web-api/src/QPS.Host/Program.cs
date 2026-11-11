using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using QPS.Crm;
using QPS.Host.Middleware;
using QPS.Host.Registration;
using QPS.Host.Storage;
using QPS.Mall;
using QPS.Persistence;
using QPS.Persistence.Database;
using QPS.Persistence.Seeds;
using QPS.Shared;
using QPS.Shared.Common;
using QPS.Shared.Storage;
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

// 文件存储：默认本地 wwwroot，配置 Storage:Provider=Oss 后切到阿里云 OSS。
builder.Services.AddQpsFileStorage(builder.Configuration);

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

if (await dbContext.Database.CanConnectAsync())
{
    // 数据库已存在：跳过建库与迁移，只补记初始迁移历史（幂等，便于后续增量迁移）。
    logger.LogInformation("数据库已存在，跳过初始化。");
    await DatabaseProvisioning.MarkInitialMigrationAppliedAsync(dbContext);
}
else
{
    // 区分「服务器不可达」与「库缺失」：连 master 查询目标库是否存在。
    // master 也连不上说明服务器不可达，按存在处理以走 FATAL 分支。
    bool databaseExists;
    try
    {
        databaseExists = connStringForDiagnostic is not null
            && await DatabaseProvisioning.DatabaseExistsAsync(connStringForDiagnostic);
    }
    catch (SqlException)
    {
        databaseExists = true;
    }

    if (databaseExists)
    {
        Console.Error.WriteLine("[FATAL] 数据库连接失败，连接串: {0}", connStringForDiagnostic ?? "无法获取");
        throw new InvalidOperationException("数据库无法连接。");
    }

    // 库缺失：自动建库并应用迁移（InitialCreate 内含 System 基线数据）。
    logger.LogInformation("数据库不存在，自动创建并写入 System 基线数据…");
    var strategy = dbContext.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () =>
    {
        await dbContext.Database.MigrateAsync();
    });
    logger.LogInformation("数据库创建完成。");
}

if (app.Environment.IsDevelopment())
{
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

// 启动即打印存储方式，便于部署后确认图片到底落在本地还是 OSS。
app.Logger.LogInformation("文件存储方式：{Provider}", app.Services.GetRequiredService<IFileStorage>().ProviderName);

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

app.MapFallback(() => Results.NotFound()).AllowAnonymous();

#endregion

app.Run();