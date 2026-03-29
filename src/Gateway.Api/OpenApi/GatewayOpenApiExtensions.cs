using Microsoft.OpenApi;
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

    public static WebApplication UseGatewayOpenApi(this WebApplication app, IEnumerable<RouteConfig> routeConfigs)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "openapi/{documentName}.json";
            options.PreSerializeFilters.Add((document, request) =>
            {
                var documentName = ResolveDocumentName(request);
                GatewayOpenApiDocumentEnricher.Enrich(document, routeConfigs, documentName);
            });
        });

        return app;
    }

    private static string? ResolveDocumentName(HttpRequest request)
    {
        if (request.RouteValues.TryGetValue("documentName", out var value) && value is not null)
        {
            return value.ToString();
        }

        var fileName = Path.GetFileName(request.Path.Value ?? string.Empty);
        return Path.GetFileNameWithoutExtension(fileName);
    }
}