namespace Gateway.Api.Security;

public sealed class GatewayAuthOptions
{
    public const string SectionName = "Gateway:Auth";

    public string Authority { get; set; } = string.Empty;

    public string Audience { get; set; } = "gateway-backoffice";

    public string ClientId { get; set; } = "gateway-backoffice";

    public string ClientSecret { get; set; } = string.Empty;

    public int IntrospectionCacheSeconds { get; set; } = 45;

    public bool RequireHttpsMetadata { get; set; } = true;
}
