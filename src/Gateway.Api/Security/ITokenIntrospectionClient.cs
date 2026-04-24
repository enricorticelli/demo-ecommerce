namespace Gateway.Api.Security;

public interface ITokenIntrospectionClient
{
    Task<IntrospectionPrincipal?> IntrospectAsync(string token, CancellationToken cancellationToken);
}
