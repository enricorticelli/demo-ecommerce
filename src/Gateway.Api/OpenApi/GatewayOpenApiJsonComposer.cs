using System.Text.Json.Nodes;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Api.OpenApi;

public static class GatewayOpenApiJsonComposer
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private static readonly HashSet<string> HttpMethods =
    [
        "get",
        "post",
        "put",
        "delete",
        "patch",
        "head",
        "options",
        "trace"
    ];

    public static async Task<JsonObject?> ComposeAsync(
        IEnumerable<RouteConfig> routeConfigs,
        IReadOnlyDictionary<string, string?> clusterAddressMap,
        string? documentName,
        CancellationToken cancellationToken)
    {
        var normalizedDocumentName = NormalizeDocumentName(documentName);
        if (normalizedDocumentName is null)
        {
            return null;
        }

        var output = new JsonObject
        {
            ["openapi"] = "3.0.4",
            ["info"] = new JsonObject
            {
                ["title"] = GetDocumentTitle(normalizedDocumentName),
                ["version"] = "v1"
            },
            ["paths"] = new JsonObject(),
            ["components"] = new JsonObject()
        };

        var sourceDocuments = new Dictionary<string, JsonObject?>(StringComparer.OrdinalIgnoreCase);
        var outputPaths = (JsonObject)output["paths"]!;

        foreach (var route in routeConfigs)
        {
            var gatewayPath = route.Match?.Path;
            if (string.IsNullOrWhiteSpace(gatewayPath) || !ShouldIncludePath(gatewayPath, normalizedDocumentName))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(route.ClusterId)
                || !clusterAddressMap.TryGetValue(route.ClusterId, out var clusterAddress)
                || string.IsNullOrWhiteSpace(clusterAddress))
            {
                continue;
            }

            var sourceDocument = await GetSourceDocumentAsync(sourceDocuments, clusterAddress!, cancellationToken);
            if (sourceDocument is null)
            {
                continue;
            }

            var downstreamPath = ToDownstreamPath(gatewayPath);
            if (downstreamPath is null
                || sourceDocument["paths"] is not JsonObject sourcePaths
                || sourcePaths[downstreamPath] is not JsonObject sourcePathItem)
            {
                continue;
            }

            if (sourceDocument["openapi"] is JsonValue sourceOpenApiVersion)
            {
                output["openapi"] = sourceOpenApiVersion.DeepClone();
            }

            var targetPathItem = outputPaths[gatewayPath] as JsonObject ?? new JsonObject();
            foreach (var method in ResolveAllowedMethods(route.Match?.Methods, sourcePathItem))
            {
                if (sourcePathItem[method] is null)
                {
                    continue;
                }

                targetPathItem[method] = sourcePathItem[method]!.DeepClone();
                if (!string.IsNullOrWhiteSpace(route.AuthorizationPolicy)
                    && targetPathItem[method] is JsonObject operation)
                {
                    ApplyBearerSecurity(output, operation);
                }
            }

            if (targetPathItem.Count > 0)
            {
                outputPaths[gatewayPath] = targetPathItem;
            }

            MergeComponents(output, sourceDocument);
        }

        return output;
    }

    private static async Task<JsonObject?> GetSourceDocumentAsync(
        IDictionary<string, JsonObject?> cache,
        string clusterAddress,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(clusterAddress, out var existing))
        {
            return existing;
        }

        var loaded = await LoadSourceDocumentAsync(clusterAddress, cancellationToken);
        cache[clusterAddress] = loaded;
        return loaded;
    }

    private static async Task<JsonObject?> LoadSourceDocumentAsync(string clusterAddress, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = new Uri(new Uri(clusterAddress, UriKind.Absolute), "openapi/v1.json");
            using var response = await HttpClient.GetAsync(documentUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken) as JsonObject;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> ResolveAllowedMethods(IEnumerable<string>? configuredMethods, JsonObject sourcePathItem)
    {
        var methods = configuredMethods?
            .Select(static method => method?.Trim().ToLowerInvariant())
            .Where(static method => !string.IsNullOrWhiteSpace(method) && HttpMethods.Contains(method!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToArray() ?? [];

        if (methods.Length > 0)
        {
            return methods;
        }

        return sourcePathItem
            .Select(static entry => entry.Key)
            .Where(static key => HttpMethods.Contains(key));
    }

    private static string? NormalizeDocumentName(string? documentName)
    {
        var normalized = (documentName ?? GatewayOpenApiDocumentNames.Full).Trim().ToLowerInvariant();

        return normalized switch
        {
            GatewayOpenApiDocumentNames.Store => GatewayOpenApiDocumentNames.Store,
            GatewayOpenApiDocumentNames.Backoffice => GatewayOpenApiDocumentNames.Backoffice,
            GatewayOpenApiDocumentNames.Full => GatewayOpenApiDocumentNames.Full,
            _ => null
        };
    }

    private static string GetDocumentTitle(string documentName)
    {
        return documentName switch
        {
            GatewayOpenApiDocumentNames.Store => "E-commerce Gateway Store API",
            GatewayOpenApiDocumentNames.Backoffice => "E-commerce Gateway Backoffice API",
            _ => "E-commerce Gateway API"
        };
    }

    private static string? ToDownstreamPath(string gatewayPath)
    {
        var segments = gatewayPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 4 || !segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var context = segments[1];
        var downstreamRest = string.Join('/', segments.Skip(3));
        return $"/{context}/{downstreamRest}";
    }

    private static bool ShouldIncludePath(string path, string documentName)
    {
        var normalizedPath = "/" + path.TrimStart('/');

        return documentName switch
        {
            GatewayOpenApiDocumentNames.Store => normalizedPath.StartsWith("/api/store/", StringComparison.OrdinalIgnoreCase),
            GatewayOpenApiDocumentNames.Backoffice => normalizedPath.StartsWith("/api/backoffice/", StringComparison.OrdinalIgnoreCase),
            GatewayOpenApiDocumentNames.Full => true,
            _ => false
        };
    }

    private static void MergeComponents(JsonObject targetDocument, JsonObject sourceDocument)
    {
        var sourceComponents = sourceDocument["components"] as JsonObject;
        if (sourceComponents is null)
        {
            return;
        }

        var targetComponents = targetDocument["components"] as JsonObject;
        if (targetComponents is null)
        {
            targetComponents = new JsonObject();
            targetDocument["components"] = targetComponents;
        }

        foreach (var section in sourceComponents)
        {
            if (section.Value is not JsonObject sourceSection)
            {
                if (!targetComponents.ContainsKey(section.Key) && section.Value is not null)
                {
                    targetComponents[section.Key] = section.Value.DeepClone();
                }

                continue;
            }

            var targetSection = targetComponents[section.Key] as JsonObject;
            if (targetSection is null)
            {
                targetComponents[section.Key] = sourceSection.DeepClone();
                continue;
            }

            foreach (var item in sourceSection)
            {
                if (!targetSection.ContainsKey(item.Key) && item.Value is not null)
                {
                    targetSection[item.Key] = item.Value.DeepClone();
                }
            }
        }
    }

    private static void ApplyBearerSecurity(JsonObject document, JsonObject operation)
    {
        EnsureBearerSecurityScheme(document);
        operation["security"] = new JsonArray
        {
            new JsonObject
            {
                ["Bearer"] = new JsonArray()
            }
        };
    }

    private static void EnsureBearerSecurityScheme(JsonObject document)
    {
        var components = document["components"] as JsonObject;
        if (components is null)
        {
            components = new JsonObject();
            document["components"] = components;
        }

        var securitySchemes = components["securitySchemes"] as JsonObject;
        if (securitySchemes is null)
        {
            securitySchemes = new JsonObject();
            components["securitySchemes"] = securitySchemes;
        }

        if (securitySchemes.ContainsKey("Bearer"))
        {
            return;
        }

        securitySchemes["Bearer"] = new JsonObject
        {
            ["type"] = "http",
            ["scheme"] = "bearer",
            ["bearerFormat"] = "opaque"
        };
    }
}
