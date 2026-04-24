using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Gateway.Api.Security;

public sealed class KeycloakIntrospectionAuthenticationHandler(
    IOptionsMonitor<KeycloakIntrospectionOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITokenIntrospectionClient introspectionClient)
    : AuthenticationHandler<KeycloakIntrospectionOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return AuthenticateResult.NoResult();
        }

        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("Missing bearer token.");
        }

        var introspectionPrincipal = await introspectionClient.IntrospectAsync(token, Context.RequestAborted);
        if (introspectionPrincipal is null)
        {
            return AuthenticateResult.Fail("Invalid or inactive bearer token.");
        }

        var ticket = new AuthenticationTicket(
            introspectionPrincipal.ToClaimsPrincipal(),
            Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers["WWW-Authenticate"] = "Bearer";
        return base.HandleChallengeAsync(properties);
    }
}
