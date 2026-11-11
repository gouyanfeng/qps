using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QPS.Host.Filters;
using QPS.Host.Storage;
using QPS.Shared.Common;
using QPS.Shared.Storage;

namespace QPS.Host.Registration;

/// <summary>
/// Host 层各个服务注册的独立入口，Program.cs 只按顺序调用。
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// 注册 MVC、全局响应包装与 JSON 序列化约定。
    /// </summary>
    public static IMvcBuilder AddQpsMvc(this IServiceCollection services)
    {
        return services
            .AddControllers(options =>
            {
                // 所有 Controller 的返回值统一经过过滤器包装成 ApiResponse，业务代码只返回原始数据。
                options.Filters.Add<ResponseWrapperFilter>();
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                // DataAnnotations 模型校验失败时与全局异常中间件保持同一响应格式：HTTP 200 + { code, msg }。
                options.InvalidModelStateResponseFactory = context =>
                {
                    var firstError = context.ModelState.Values
                        .SelectMany(state => state.Errors)
                        .Select(error => string.IsNullOrEmpty(error.ErrorMessage) ? "请求参数不合法" : error.ErrorMessage)
                        .FirstOrDefault() ?? "请求参数不合法";

                    return new Microsoft.AspNetCore.Mvc.OkObjectResult(
                        QPS.Shared.Common.ApiResponse<object>.Fail(400, firstError));
                };
            })
            .AddJsonOptions(options =>
            {
                // 字段名输出为 camelCase，与前端约定保持一致。
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                // 关闭非 ASCII 字符转义，中文在接口响应中保持原文，避免前端看到 \uXXXX。
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            });
    }

    /// <summary>
    /// 限定 Controller 的发现范围：只保留 Host 自身，业务模块由各自的 AddQpsXxx() 加入。
    /// 未调用 AddQpsXxx() 的模块因此不会暴露 Controller，也不会注册 Handler。
    /// </summary>
    public static IMvcBuilder ConfigureQpsApplicationParts(this IMvcBuilder mvcBuilder, Assembly hostAssembly)
    {
        mvcBuilder.ConfigureApplicationPartManager(manager =>
        {
            manager.ApplicationParts.Clear();
            manager.ApplicationParts.Add(new AssemblyPart(hostAssembly));
        });

        return mvcBuilder;
    }

    /// <summary>
    /// 注册 CORS，策略名保持 AllowAll，允许任意来源、方法与请求头。
    /// </summary>
    public static IServiceCollection AddQpsCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }

    /// <summary>
    /// 注册 Swagger 文档，并提供 Bearer 令牌输入入口。仅由开发环境调用。
    /// </summary>
    public static IServiceCollection AddQpsSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "QPS API", Version = "v1" });
            c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    /// <summary>
    /// 注册文件存储：Storage:Provider=Oss 时走阿里云 OSS，否则写本地 wwwroot。
    /// 两种实现返回同一套 /uploads/... 相对地址，前端无需区分。
    /// </summary>
    public static IServiceCollection AddQpsFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(FileStorageOptions.SectionName);
        services.Configure<FileStorageOptions>(section);

        var storageOptions = section.Get<FileStorageOptions>() ?? new FileStorageOptions();

        if (storageOptions.UseOss)
        {
            var oss = storageOptions.Oss;

            // 缺配置在启动阶段就失败，避免上传时才暴露成 500。
            if (string.IsNullOrWhiteSpace(oss.Endpoint) ||
                string.IsNullOrWhiteSpace(oss.BucketName) ||
                string.IsNullOrWhiteSpace(oss.AccessKeyId) ||
                string.IsNullOrWhiteSpace(oss.AccessKeySecret))
            {
                throw new InvalidOperationException(
                    "Storage:Provider=Oss 时必须配置 Oss:Endpoint、BucketName、AccessKeyId、AccessKeySecret。");
            }

            services.AddSingleton<IFileStorage, OssFileStorage>();
        }
        else
        {
            services.AddSingleton<IFileStorage, LocalFileStorage>();
        }

        return services;
    }

    /// <summary>
    /// 注册 JWT Bearer 鉴权，仅校验签名与有效期；签发者/受众不做校验。
    /// </summary>
    public static IServiceCollection AddQpsJwtAuthentication(
        this IServiceCollection services,
        JwtOptions jwtOptions)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    RoleClaimType = "role",
                    NameClaimType = "username"
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
            options.AddPolicy("MallMember", policy => policy.RequireRole("member"));
            options.AddPolicy("AnyUser", policy => policy.RequireAuthenticatedUser());
        });

        return services;
    }
}