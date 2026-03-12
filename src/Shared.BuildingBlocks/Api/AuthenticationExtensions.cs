using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.BuildingBlocks.Api;

public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddStoreAndAdminAuthentication(this WebApplicationBuilder builder)
    {
        var signingKey = builder.Configuration["Account:Jwt:SigningKey"] ?? "dev-only-signing-key-change-me";
        var adminIssuer = builder.Configuration["Account:Jwt:AdminIssuer"] ?? "account-admin";
        var adminAudience = builder.Configuration["Account:Jwt:AdminAudience"] ?? "backoffice";
        var customerIssuer = builder.Configuration["Account:Jwt:CustomerIssuer"] ?? "account-customer";
        var customerAudience = builder.Configuration["Account:Jwt:CustomerAudience"] ?? "storefront";

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateIssuer = true,
                    ValidIssuers = [adminIssuer, customerIssuer],
                    ValidateAudience = true,
                    ValidAudiences = [adminAudience, customerAudience],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim("realm", "admin"));
            options.AddPolicy("CustomerPolicy", policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim("realm", "customer"));
        });

        return builder;
    }

    public static WebApplicationBuilder AddCustomerAuthentication(this WebApplicationBuilder builder)
    {
        return builder.AddJwtAuthentication("customer", "CustomerPolicy");
    }

    public static WebApplicationBuilder AddAdminAuthentication(this WebApplicationBuilder builder)
    {
        return builder.AddJwtAuthentication("admin", "AdminPolicy");
    }

    private static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder, string realm, string policyName)
    {
        var signingKey = builder.Configuration["Account:Jwt:SigningKey"] ?? "dev-only-signing-key-change-me";
        var normalizedRealm = realm.Trim().ToLowerInvariant();
        var issuer = normalizedRealm == "admin"
            ? builder.Configuration["Account:Jwt:AdminIssuer"] ?? "account-admin"
            : builder.Configuration["Account:Jwt:CustomerIssuer"] ?? "account-customer";
        var audience = normalizedRealm == "admin"
            ? builder.Configuration["Account:Jwt:AdminAudience"] ?? "backoffice"
            : builder.Configuration["Account:Jwt:CustomerAudience"] ?? "storefront";

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(policyName, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim("realm", normalizedRealm));
        });

        return builder;
    }
}
