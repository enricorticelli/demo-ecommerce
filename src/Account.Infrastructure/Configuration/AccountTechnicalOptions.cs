using Microsoft.Extensions.Configuration;

namespace Account.Infrastructure.Configuration;

public sealed class AccountTechnicalOptions
{
    public string AccountConnectionString { get; init; } = string.Empty;
    public string JwtSigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 30;
    public int RefreshTokenDays { get; init; } = 14;
    public string CustomerIssuer { get; init; } = "account-customer";
    public string CustomerAudience { get; init; } = "storefront";
    public string AdminIssuer { get; init; } = "account-admin";
    public string AdminAudience { get; init; } = "backoffice";
    public string OrderApiBaseUrl { get; init; } = "http://order-api:8080";
    public string OrderInternalApiKey { get; init; } = "dev-order-internal-key-change-me";
    public string DefaultAdminUsername { get; init; } = "admin";
    public string DefaultAdminPassword { get; init; } = "admin";

    public static AccountTechnicalOptions FromConfiguration(IConfiguration configuration)
    {
        return new AccountTechnicalOptions
        {
            AccountConnectionString = ResolveAccountConnectionString(configuration),
            JwtSigningKey = configuration["Account:Jwt:SigningKey"] ?? "dev-only-signing-key-change-me",
            AccessTokenMinutes = ParseInt(configuration["Account:Jwt:AccessTokenMinutes"], 30),
            RefreshTokenDays = ParseInt(configuration["Account:Jwt:RefreshTokenDays"], 14),
            CustomerIssuer = configuration["Account:Jwt:CustomerIssuer"] ?? "account-customer",
            CustomerAudience = configuration["Account:Jwt:CustomerAudience"] ?? "storefront",
            AdminIssuer = configuration["Account:Jwt:AdminIssuer"] ?? "account-admin",
            AdminAudience = configuration["Account:Jwt:AdminAudience"] ?? "backoffice",
            OrderApiBaseUrl = configuration["Account:OrderApiBaseUrl"] ?? "http://order-api:8080",
            OrderInternalApiKey = configuration["Account:OrderInternalApiKey"] ?? "dev-order-internal-key-change-me",
            DefaultAdminUsername = configuration["Account:Admin:Username"] ?? "admin",
            DefaultAdminPassword = configuration["Account:Admin:Password"] ?? "admin"
        };
    }

    public static string ResolveAccountConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("AccountDb")
            ?? throw new InvalidOperationException("Connection string 'AccountDb' is missing.");
    }

    private static int ParseInt(string? raw, int fallback)
    {
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : fallback;
    }
}
