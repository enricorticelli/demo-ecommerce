using Microsoft.AspNetCore.Authentication;

namespace Gateway.Api.Security;

public sealed class KeycloakIntrospectionOptions : AuthenticationSchemeOptions
{
    public string Authority { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public int CacheSeconds { get; set; } = 45;

    public bool RequireHttpsMetadata { get; set; } = true;

    public Uri IntrospectionEndpoint
    {
        get
        {
            var authority = Authority.TrimEnd('/');
            return new Uri($"{authority}/protocol/openid-connect/token/introspect", UriKind.Absolute);
        }
    }
}
