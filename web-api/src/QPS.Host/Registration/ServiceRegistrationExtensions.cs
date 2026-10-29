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
using QPS.Shared.Common;

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
    /// 注册 Swagger 文档，并提供 Bearer 令牌输入入口。
    /// </summary>
    public static IServiceCollection AddQpsSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "QPS API", Version = "v1" });
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
    /// 注册 JWT Bearer 鉴权，密钥、签发者与受众由 Host 启动时统一绑定。
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
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
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