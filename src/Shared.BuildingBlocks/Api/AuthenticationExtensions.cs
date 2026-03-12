using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
            options.AddPolicy(AuthorizationPolicies.AdminPolicy, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim(AuthorizationClaimTypes.Realm, "admin"));
            options.AddPolicy(AuthorizationPolicies.CustomerPolicy, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim(AuthorizationClaimTypes.Realm, "customer"));

            AddAdminPermissionPolicies(options);
        });

        return builder;
    }

    public static WebApplicationBuilder AddCustomerAuthentication(this WebApplicationBuilder builder)
    {
        return builder.AddJwtAuthentication("customer", AuthorizationPolicies.CustomerPolicy);
    }

    public static WebApplicationBuilder AddAdminAuthentication(this WebApplicationBuilder builder)
    {
        return builder.AddJwtAuthentication("admin", AuthorizationPolicies.AdminPolicy);
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
                    .RequireClaim(AuthorizationClaimTypes.Realm, normalizedRealm));

            if (normalizedRealm == "admin")
            {
                AddAdminPermissionPolicies(options);
            }
        });

        return builder;
    }

    private static void AddAdminPermissionPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(AuthorizationPolicies.CatalogReadPolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, "admin")
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.CatalogRead));

        options.AddPolicy(AuthorizationPolicies.CatalogWritePolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, "admin")
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.CatalogWrite));

        options.AddPolicy(AuthorizationPolicies.OrdersReadPolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, "admin")
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.OrdersRead));

        options.AddPolicy(AuthorizationPolicies.OrdersWritePolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, "admin")
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.OrdersWrite));

        options.AddPolicy(AuthorizationPolicies.ShippingReadPolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, "admin")
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.ShippingRead));

        options.AddPolicy(AuthorizationPolicies.ShippingWritePolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, "admin")
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.ShippingWrite));

        options.AddPolicy(AuthorizationPolicies.WarehouseReadPolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, "admin")
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.WarehouseRead));

        options.AddPolicy(AuthorizationPolicies.WarehouseWritePolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, "admin")
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.WarehouseWrite));

        options.AddPolicy(AuthorizationPolicies.AccountAdminReadPolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, "admin")
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.AccountRead));

        options.AddPolicy(AuthorizationPolicies.AccountAdminWritePolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, "admin")
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.AccountWrite));
    }
}
