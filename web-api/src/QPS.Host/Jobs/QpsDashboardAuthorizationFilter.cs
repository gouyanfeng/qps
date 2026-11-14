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
/// 3. 首次用查询参数通过校验后落 HttpOnly Cookie：Dashboard 页面内部的 XHR
///    （recurring / stats 等）不会带查询参数，只能靠同源 Cookie 延续授权，
///    否则表现为「页面能打开但表格全空、控制台一串 401」；
/// 4. 以上都不满足一律拒绝，避免生产环境暴露任务看板。
/// </summary>
public sealed class QpsDashboardAuthorizationFilter(JwtOptions jwtOptions) : IDashboardAuthorizationFilter
{
    private const string CookieName = "qps_jobs_token";
    private const string CookiePath = "/jobs-dashboard";

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp is not null &&
            (IPAddress.IsLoopback(remoteIp) || remoteIp.Equals(IPAddress.IPv6Loopback)))
        {
            return true;
        }

        // 查询参数优先（首次进入），其次读 Cookie（页面内 XHR）
        var accessToken = httpContext.Request.Query["access_token"].ToString();
        var fromQuery = !string.IsNullOrEmpty(accessToken);
        if (!fromQuery)
        {
            accessToken = httpContext.Request.Cookies[CookieName] ?? string.Empty;
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            return false;
        }

        if (!TryValidate(accessToken, out var expiresAt))
        {
            return false;
        }

        if (fromQuery && expiresAt is not null)
        {
            httpContext.Response.Cookies.Append(CookieName, accessToken, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Path = CookiePath,
                Expires = expiresAt,
                // HTTP 环境下不能加 Secure，否则浏览器不保存
                Secure = httpContext.Request.IsHttps
            });
        }

        return true;
    }

    /// <summary>校验签名/有效期并要求 admin 角色，输出令牌过期时间供 Cookie 设置。</summary>
    private bool TryValidate(string accessToken, out DateTimeOffset? expiresAt)
    {
        expiresAt = null;
        try
        {
            // 校验规则与 API 主链路一致：只验签名与有效期，要求 admin 角色。
            // MapInboundClaims 必须关掉：默认映射会把 role 改写成 ClaimTypes.Role 长名，
            // 导致 RoleClaimType="role" 匹配不到，远程访问恒 401。
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(
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
                out var validatedToken);

            if (!principal.IsInRole("admin"))
            {
                return false;
            }

            if (validatedToken is JwtSecurityToken jwt && jwt.ValidTo != DateTime.MinValue)
            {
                expiresAt = new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
