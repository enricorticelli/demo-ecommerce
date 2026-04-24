using System.Net;
using System.Text;
using Gateway.Api.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gateway.Tests;

public sealed class KeycloakTokenIntrospectionClientTests
{
    [Fact]
    public async Task IntrospectAsync_InactiveToken_ReturnsNull()
    {
        var sut = CreateClient("""{"active":false}""");

        var result = await sut.IntrospectAsync("opaque-token", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task IntrospectAsync_ActiveToken_ExtractsCapabilitiesFromIntrospectionResponse()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var sut = CreateClient(
            $$"""
            {
              "active": true,
              "sub": "user-1",
              "iss": "http://keycloak:8080/realms/demo-ecommerce",
              "aud": ["gateway-backoffice"],
              "exp": {{expiresAt}},
              "resource_access": {
                "gateway-backoffice": {
                  "roles": ["catalog.products.read", "catalog.*"]
                }
              }
            }
            """);

        var result = await sut.IntrospectAsync("opaque-token", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("user-1", result.Subject);
        Assert.Contains(CapabilitySecurity.CatalogProductsRead, result.Capabilities);
        Assert.Contains("catalog.*", result.Capabilities);
    }

    private static KeycloakTokenIntrospectionClient CreateClient(string responseBody)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responseBody))
        {
            BaseAddress = new Uri("http://keycloak:8080")
        };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = new TestOptionsMonitor<KeycloakIntrospectionOptions>(
            new KeycloakIntrospectionOptions
            {
                Authority = "http://keycloak:8080/realms/demo-ecommerce",
                Audience = "gateway-backoffice",
                ClientId = "gateway-backoffice",
                ClientSecret = "local-dev-gateway-secret",
                RequireHttpsMetadata = false
            });

        return new KeycloakTokenIntrospectionClient(httpClient, cache, options);
    }

    private sealed class StubHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    private sealed class TestOptionsMonitor<TOptions>(TOptions value) : IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue => value;

        public TOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
    }
}
