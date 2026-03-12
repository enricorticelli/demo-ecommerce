using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Account.Application.Models;
using Account.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Account.Infrastructure.Services;

public sealed class TokenFactory(AccountTechnicalOptions options)
{
    private readonly SymmetricSecurityKey _securityKey = new(Encoding.UTF8.GetBytes(options.JwtSigningKey));

    public (string AccessToken, DateTimeOffset ExpiresAtUtc) CreateAccessToken(
        Guid userId,
        string email,
        bool emailVerified,
        string realm,
        string[] permissions,
        bool isSuperUser)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(options.AccessTokenMinutes);
        var role = realm == AccountRealm.Admin ? "admin" : "customer";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("realm", realm),
            new("role", role),
            new("email_verified", emailVerified ? "true" : "false"),
            new("super_user", isSuperUser ? "true" : "false")
        };

        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: realm == AccountRealm.Admin ? options.AdminIssuer : options.CustomerIssuer,
            audience: realm == AccountRealm.Admin ? options.AdminAudience : options.CustomerAudience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string RefreshToken, DateTimeOffset ExpiresAtUtc) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        var token = Convert.ToBase64String(bytes);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(options.RefreshTokenDays);
        return (token, expiresAt);
    }

    public TokenValidationParameters CreateValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = [options.CustomerIssuer, options.AdminIssuer],
            ValidateAudience = true,
            ValidAudiences = [options.CustomerAudience, options.AdminAudience],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _securityKey,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };
    }
}
