using System.Net.Http;
using Microsoft.OpenApi;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Api.OpenApi;

public static class GatewayOpenApiDocumentEnricher
{
    public static void Enrich(OpenApiDocument document, IEnumerable<RouteConfig> routeConfigs, string? documentName)
    {
        foreach (var route in routeConfigs)
        {
            var path = route.Match?.Path;
            if (string.IsNullOrWhiteSpace(path) || !ShouldIncludePath(path, documentName))
            {
                continue;
            }

            if (!document.Paths.TryGetValue(path, out var pathItem))
            {
                pathItem = new OpenApiPathItem();
                document.Paths[path] = pathItem;
            }

            var operations = pathItem.Operations;
            if (operations is null)
            {
                continue;
            }

            foreach (var method in route.Match?.Methods ?? [])
            {
                if (!TryGetHttpMethod(method, out var httpMethod) || operations.ContainsKey(httpMethod))
                {
                    continue;
                }

                operations[httpMethod] = new OpenApiOperation
                {
                    OperationId = route.RouteId,
                    Summary = $"Gateway proxy for {method} {path}",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse { Description = "Proxied response" }
                    }
                };
            }
        }
    }

    private static bool ShouldIncludePath(string path, string? documentName)
    {
        var normalizedPath = "/" + path.TrimStart('/');

        return documentName?.ToLowerInvariant() switch
        {
            GatewayOpenApiDocumentNames.Store => normalizedPath.StartsWith("/api/store/", StringComparison.OrdinalIgnoreCase),
            GatewayOpenApiDocumentNames.Backoffice => normalizedPath.StartsWith("/api/backoffice/", StringComparison.OrdinalIgnoreCase),
            GatewayOpenApiDocumentNames.Full or null or "" => true,
            _ => false
        };
    }

    private static bool TryGetHttpMethod(string method, out HttpMethod httpMethod)
    {
        switch (method.ToUpperInvariant())
        {
            case "GET":
                httpMethod = HttpMethod.Get;
                return true;
            case "POST":
                httpMethod = HttpMethod.Post;
                return true;
            case "PUT":
                httpMethod = HttpMethod.Put;
                return true;
            case "DELETE":
                httpMethod = HttpMethod.Delete;
                return true;
            case "PATCH":
                httpMethod = HttpMethod.Patch;
                return true;
            case "HEAD":
                httpMethod = HttpMethod.Head;
                return true;
            case "OPTIONS":
                httpMethod = HttpMethod.Options;
                return true;
            case "TRACE":
                httpMethod = HttpMethod.Trace;
                return true;
            default:
                httpMethod = HttpMethod.Get;
                return false;
        }
    }
}