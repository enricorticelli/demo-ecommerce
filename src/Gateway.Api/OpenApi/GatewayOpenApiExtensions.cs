using Microsoft.OpenApi;
using System.Text.Json;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Api.OpenApi;

public static class GatewayOpenApiExtensions
{
    public static IServiceCollection AddGatewayOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(GatewayOpenApiDocumentNames.Full, new OpenApiInfo
            {
                Title = "E-commerce Gateway API",
                Version = "v1"
            });

            options.SwaggerDoc(GatewayOpenApiDocumentNames.Store, new OpenApiInfo
            {
                Title = "E-commerce Gateway Store API",
                Version = "v1"
            });

            options.SwaggerDoc(GatewayOpenApiDocumentNames.Backoffice, new OpenApiInfo
            {
                Title = "E-commerce Gateway Backoffice API",
                Version = "v1"
            });

            options.DocInclusionPredicate(static (documentName, apiDescription) =>
            {
                var relativePath = apiDescription.RelativePath;
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    return false;
                }

                var normalizedPath = "/" + relativePath.TrimStart('/');

                return documentName switch
                {
                    GatewayOpenApiDocumentNames.Store => normalizedPath.StartsWith("/api/store/", StringComparison.OrdinalIgnoreCase),
                    GatewayOpenApiDocumentNames.Backoffice => normalizedPath.StartsWith("/api/backoffice/", StringComparison.OrdinalIgnoreCase),
                    GatewayOpenApiDocumentNames.Full => true,
                    _ => false
                };
            });
        });

        return services;
    }

    public static WebApplication UseGatewayOpenApi(
        this WebApplication app,
        IEnumerable<RouteConfig> routeConfigs,
        IEnumerable<ClusterConfig> clusterConfigs)
    {
        var clusterAddressMap = clusterConfigs
            .Where(cluster => !string.IsNullOrWhiteSpace(cluster.ClusterId))
            .ToDictionary(
                cluster => cluster.ClusterId!,
                cluster => cluster.Destinations?.Values.FirstOrDefault()?.Address,
                StringComparer.OrdinalIgnoreCase);

        app.MapGet("/openapi/{documentName}.json", async (string documentName, CancellationToken cancellationToken) =>
        {
            var normalizedDocumentName = Path.GetFileNameWithoutExtension(documentName);
            var merged = await GatewayOpenApiJsonComposer.ComposeAsync(
                routeConfigs,
                clusterAddressMap,
                normalizedDocumentName,
                cancellationToken);

            if (merged is null)
            {
                return Results.NotFound();
            }

            return Results.Text(merged.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            }), "application/json");
        });

        return app;
    }
}