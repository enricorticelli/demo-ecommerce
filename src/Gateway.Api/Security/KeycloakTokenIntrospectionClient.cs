using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Gateway.Api.Security;

public sealed class KeycloakTokenIntrospectionClient(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptionsMonitor<KeycloakIntrospectionOptions> optionsMonitor)
    : ITokenIntrospectionClient
{
    public async Task<IntrospectionPrincipal?> IntrospectAsync(string token, CancellationToken cancellationToken)
    {
        var options = optionsMonitor.Get(GatewayAuthenticationDefaults.Scheme);
        ValidateOptions(options);

        var cacheKey = $"gateway:introspection:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(token)))}";
        if (cache.TryGetValue(cacheKey, out IntrospectionPrincipal? cachedPrincipal))
        {
            return cachedPrincipal;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, options.IntrospectionEndpoint)
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("token", token),
                new KeyValuePair<string, string>("token_type_hint", "access_token")
            ])
        };

        var basicValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicValue);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonNode.ParseAsync(responseStream, cancellationToken: cancellationToken) as JsonObject;
        if (payload is null || !IsActive(payload))
        {
            return null;
        }

        var principal = CreatePrincipal(payload, options);
        if (principal is null)
        {
            return null;
        }

        var cacheSeconds = Math.Clamp(options.CacheSeconds, 0, 300);
        if (cacheSeconds > 0)
        {
            var expiresIn = principal.ExpiresAt - DateTimeOffset.UtcNow;
            var cacheDuration = TimeSpan.FromSeconds(cacheSeconds);
            var absoluteExpirationRelativeToNow = expiresIn < cacheDuration ? expiresIn : cacheDuration;
            if (absoluteExpirationRelativeToNow > TimeSpan.Zero)
            {
                cache.Set(cacheKey, principal, absoluteExpirationRelativeToNow);
            }
        }

        return principal;
    }

    private static IntrospectionPrincipal? CreatePrincipal(JsonObject payload, KeycloakIntrospectionOptions options)
    {
        var subject = GetString(payload, "sub");
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        if (!IssuerMatches(payload, options) || !AudienceMatches(payload, options))
        {
            return null;
        }

        var expiresAt = GetUnixTime(payload, "exp");
        if (!expiresAt.HasValue || expiresAt.Value <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var capabilities = ExtractCapabilities(payload, options.Audience)
            .Where(static capability => !string.IsNullOrWhiteSpace(capability))
            .Select(static capability => capability.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new IntrospectionPrincipal(subject, expiresAt.Value, capabilities);
    }

    private static bool IsActive(JsonObject payload)
    {
        return payload["active"]?.GetValue<bool>() == true;
    }

    private static bool IssuerMatches(JsonObject payload, KeycloakIntrospectionOptions options)
    {
        var issuer = GetString(payload, "iss")?.TrimEnd('/');
        var expectedIssuer = options.Authority.TrimEnd('/');
        return string.Equals(issuer, expectedIssuer, StringComparison.Ordinal);
    }

    private static bool AudienceMatches(JsonObject payload, KeycloakIntrospectionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return true;
        }

        if (StringArrayContains(payload["aud"], options.Audience))
        {
            return true;
        }

        var clientId = GetString(payload, "client_id");
        var authorizedParty = GetString(payload, "azp");
        return string.Equals(clientId, options.Audience, StringComparison.Ordinal)
            || string.Equals(authorizedParty, options.Audience, StringComparison.Ordinal);
    }

    private static IEnumerable<string> ExtractCapabilities(JsonObject payload, string audience)
    {
        if (payload["capabilities"] is JsonArray capabilities)
        {
            foreach (var capability in ReadStringArray(capabilities))
            {
                yield return capability;
            }
        }

        if (payload["resource_access"] is not JsonObject resourceAccess)
        {
            yield break;
        }

        foreach (var capability in ExtractClientRoles(resourceAccess, audience))
        {
            yield return capability;
        }
    }

    private static IEnumerable<string> ExtractClientRoles(JsonObject resourceAccess, string clientId)
    {
        if (resourceAccess[clientId] is JsonObject clientAccess
            && clientAccess["roles"] is JsonArray roles)
        {
            foreach (var role in ReadStringArray(roles))
            {
                yield return role;
            }
        }
    }

    private static IEnumerable<string> ReadStringArray(JsonArray array)
    {
        foreach (var item in array)
        {
            var value = item?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }
    }

    private static bool StringArrayContains(JsonNode? node, string expected)
    {
        if (node is JsonArray array)
        {
            return ReadStringArray(array).Contains(expected, StringComparer.Ordinal);
        }

        var value = node?.GetValue<string>();
        return string.Equals(value, expected, StringComparison.Ordinal);
    }

    private static string? GetString(JsonObject payload, string propertyName)
    {
        return payload[propertyName]?.GetValue<string>();
    }

    private static DateTimeOffset? GetUnixTime(JsonObject payload, string propertyName)
    {
        var value = payload[propertyName];
        if (value is null)
        {
            return null;
        }

        if (value is JsonValue jsonValue && jsonValue.TryGetValue<long>(out var unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }

        return null;
    }

    private static void ValidateOptions(KeycloakIntrospectionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Authority))
        {
            throw new InvalidOperationException("Gateway auth authority is required.");
        }

        if (options.RequireHttpsMetadata
            && !options.Authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Gateway auth authority must use HTTPS unless disabled for local development.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new InvalidOperationException("Gateway auth introspection client credentials are required.");
        }
    }
}
