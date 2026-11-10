namespace QPS.Shared.Common;

/// <summary>
/// JWT 配置：Host 的令牌校验与 Persistence 的令牌签发共用同一份，
/// 避免两处各自维护默认值导致密钥不一致。
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; init; } = string.Empty;
}
