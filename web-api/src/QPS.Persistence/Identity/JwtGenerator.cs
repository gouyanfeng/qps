using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using QPS.Shared.Common;
using QPS.Shared.Contracts;

namespace QPS.Persistence.Identity;

public class JwtGenerator : IJwtGenerator
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtGenerator(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _secretKey = string.IsNullOrWhiteSpace(options.SecretKey)
            ? throw new ArgumentException("Jwt SecretKey 不能为空。", nameof(options))
            : options.SecretKey;
        _issuer = string.IsNullOrWhiteSpace(options.Issuer)
            ? throw new ArgumentException("Jwt Issuer 不能为空。", nameof(options))
            : options.Issuer;
        _audience = string.IsNullOrWhiteSpace(options.Audience)
            ? throw new ArgumentException("Jwt Audience 不能为空。", nameof(options))
            : options.Audience;
    }

    public string GenerateToken(Guid userId, string username, string role)
    {
        var claims = new List<Claim>
        {
            new Claim("userId", userId.ToString()),
            new Claim("username", username),
            new Claim("role", role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            expires: DateTime.UtcNow.AddDays(10),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}


