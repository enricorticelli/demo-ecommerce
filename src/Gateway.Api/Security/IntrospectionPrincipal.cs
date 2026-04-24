using System.Security.Claims;

namespace Gateway.Api.Security;

public sealed record IntrospectionPrincipal(
    string Subject,
    DateTimeOffset ExpiresAt,
    IReadOnlyCollection<string> Capabilities)
{
    public ClaimsPrincipal ToClaimsPrincipal()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Subject),
            new("sub", Subject)
        };

        claims.AddRange(Capabilities.Select(static capability =>
            new Claim(CapabilitySecurity.ClaimType, capability)));

        var identity = new ClaimsIdentity(claims, GatewayAuthenticationDefaults.Scheme);
        return new ClaimsPrincipal(identity);
    }
}
