using Gateway.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Api.Extensions;

public static class GatewayServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayCoreServices(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddMemoryCache();
        services.AddHttpClient<ITokenIntrospectionClient, KeycloakTokenIntrospectionClient>();
        services.AddSingleton<IAuthorizationHandler, CapabilityAuthorizationHandler>();
        
        services.AddOptions<GatewayAuthOptions>()
            .BindConfiguration(GatewayAuthOptions.SectionName);
        services.AddAuthentication(GatewayAuthenticationDefaults.Scheme)
            .AddScheme<KeycloakIntrospectionOptions, KeycloakIntrospectionAuthenticationHandler>(
                GatewayAuthenticationDefaults.Scheme,
                options => { });
        
        services.AddOptions<KeycloakIntrospectionOptions>(GatewayAuthenticationDefaults.Scheme)
            .Configure<IConfiguration>((options, configuration) =>
            {
                var gatewayOptions = configuration
                    .GetSection(GatewayAuthOptions.SectionName)
                    .Get<GatewayAuthOptions>() ?? new GatewayAuthOptions();

                options.Authority = gatewayOptions.Authority;
                options.Audience = gatewayOptions.Audience;
                options.ClientId = gatewayOptions.ClientId;
                options.ClientSecret = gatewayOptions.ClientSecret;
                options.CacheSeconds = gatewayOptions.IntrospectionCacheSeconds;
                options.RequireHttpsMetadata = gatewayOptions.RequireHttpsMetadata;
            });
        
        services.AddAuthorization(options =>
        {
            AddCapabilityPolicy(options, CapabilitySecurity.CatalogProductsRead);
            AddCapabilityPolicy(options, CapabilitySecurity.CatalogProductsWrite);
            AddCapabilityPolicy(options, CapabilitySecurity.CatalogBrandsRead);
            AddCapabilityPolicy(options, CapabilitySecurity.CatalogBrandsWrite);
            AddCapabilityPolicy(options, CapabilitySecurity.CatalogCategoriesRead);
            AddCapabilityPolicy(options, CapabilitySecurity.CatalogCategoriesWrite);
            AddCapabilityPolicy(options, CapabilitySecurity.CatalogCollectionsRead);
            AddCapabilityPolicy(options, CapabilitySecurity.CatalogCollectionsWrite);
            AddCapabilityPolicy(options, CapabilitySecurity.WarehouseStockRead);
            AddCapabilityPolicy(options, CapabilitySecurity.WarehouseStockWrite);
        });
        
        services.AddCors(options =>
        {
            options.AddPolicy("default", policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        return services;
    }

    private static void AddCapabilityPolicy(AuthorizationOptions options, string capability)
    {
        options.AddPolicy(
            CapabilitySecurity.PolicyName(capability),
            policy => policy
                .AddAuthenticationSchemes(GatewayAuthenticationDefaults.Scheme)
                .RequireAuthenticatedUser()
                .AddRequirements(new CapabilityAuthorizationRequirement(capability)));
    }

    public static IServiceCollection AddGatewayReverseProxy(
        this IServiceCollection services,
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters)
    {
        services.AddReverseProxy().LoadFromMemory(routes, clusters);
        return services;
    }
}
