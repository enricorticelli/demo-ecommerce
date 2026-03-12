using Account.Application.Abstractions.Services;
using Account.Application.Models;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Api;

namespace Account.Infrastructure.Configuration;

public static class AccountInfrastructureExtensions
{
    public static WebApplicationBuilder AddAccountModule(this WebApplicationBuilder builder)
    {
        var options = AccountTechnicalOptions.FromConfiguration(builder.Configuration);

        builder.Services.AddSingleton(options);
        builder.Services.AddDbContext<AccountDbContext>(db => db.UseNpgsql(options.AccountConnectionString));

        builder.Services.AddScoped<IAccountAuthService, AccountAuthService>();
        builder.Services.AddScoped<IAccountCustomerProfileService, AccountCustomerProfileService>();
        builder.Services.AddScoped<IAccountAdministrationService, AccountAdministrationService>();
        builder.Services.AddScoped<IAccountBootstrapService, AccountBootstrapService>();
        builder.Services.AddScoped<TokenFactory>();

        builder.Services.AddHttpClient<OrderApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.OrderApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.TokenValidationParameters = new TokenFactory(options).CreateValidationParameters();
            });

        builder.Services.AddAuthorization(optionsAuth =>
        {
            optionsAuth.AddPolicy(AuthorizationPolicies.CustomerPolicy, policy =>
                policy.RequireAuthenticatedUser().RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Customer));
            optionsAuth.AddPolicy(AuthorizationPolicies.AdminPolicy, policy =>
                policy.RequireAuthenticatedUser().RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Admin));

            AddAdminPermissionPolicies(optionsAuth);
        });

        return builder;
    }

    private static void AddAdminPermissionPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(AuthorizationPolicies.CatalogReadPolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Admin)
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.CatalogRead));

        options.AddPolicy(AuthorizationPolicies.CatalogWritePolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Admin)
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.CatalogWrite));

        options.AddPolicy(AuthorizationPolicies.OrdersReadPolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Admin)
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.OrdersRead));

        options.AddPolicy(AuthorizationPolicies.OrdersWritePolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Admin)
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.OrdersWrite));

        options.AddPolicy(AuthorizationPolicies.ShippingReadPolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Admin)
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.ShippingRead));

        options.AddPolicy(AuthorizationPolicies.ShippingWritePolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Admin)
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.ShippingWrite));

        options.AddPolicy(AuthorizationPolicies.WarehouseReadPolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Admin)
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.WarehouseRead));

        options.AddPolicy(AuthorizationPolicies.WarehouseWritePolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Admin)
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.WarehouseWrite));

        options.AddPolicy(AuthorizationPolicies.AccountAdminReadPolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Admin)
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.AccountRead));

        options.AddPolicy(AuthorizationPolicies.AccountAdminWritePolicy, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaimTypes.Realm, AccountRealm.Admin)
                .RequireClaim(AuthorizationClaimTypes.Permission, AuthorizationPermissions.AccountWrite));
    }

    public static async Task UseAccountModuleAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var dbContext = services.GetRequiredService<AccountDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE accounting.users ADD COLUMN IF NOT EXISTS \"CustomPermissions\" text[] NULL;");

        var options = services.GetRequiredService<AccountTechnicalOptions>();
        var bootstrapService = services.GetRequiredService<IAccountBootstrapService>();
        await bootstrapService.EnsureDefaultAdminAsync(options.DefaultAdminUsername, options.DefaultAdminPassword, CancellationToken.None);
    }
}
