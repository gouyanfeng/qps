using System.Net;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Hangfire.Dashboard;
using Microsoft.IdentityModel.Tokens;
using QPS.Shared.Common;

namespace QPS.Host.Jobs;

/// <summary>
/// Hangfire Dashboard 访问控制：
/// 1. 本机（回环地址）访问始终放行，便于本地调试；
/// 2. 远程访问凭 ?access_token= 携带的管理端 JWT 放行（要求 admin 角色）——
///    iframe 无法自定义请求头，令牌只能走查询参数，与 SignalR /hubs 同款约定；
/// 3. 两项都不满足一律拒绝，避免生产环境暴露任务看板。
/// </summary>
public sealed class QpsDashboardAuthorizationFilter(JwtOptions jwtOptions) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp is not null &&
            (IPAddress.IsLoopback(remoteIp) || remoteIp.Equals(IPAddress.IPv6Loopback)))
        {
            return true;
        }

        var accessToken = httpContext.Request.Query["access_token"].ToString();
        if (string.IsNullOrEmpty(accessToken))
        {
            return false;
        }

        try
        {
            // 校验规则与 API 主链路一致：只验签名与有效期，要求 admin 角色。
            var principal = new JwtSecurityTokenHandler().ValidateToken(
                accessToken,
                new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    RoleClaimType = "role",
                    NameClaimType = "username"
                },
                out _);

            return principal.IsInRole("admin");
        }
        catch
        {
            return false;
        }
    }
}
