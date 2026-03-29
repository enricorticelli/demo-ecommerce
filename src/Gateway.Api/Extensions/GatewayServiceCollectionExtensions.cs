using Yarp.ReverseProxy.Configuration;

namespace Gateway.Api.Extensions;

public static class GatewayServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayCoreServices(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddCors(options =>
        {
            options.AddPolicy("default", policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        return services;
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