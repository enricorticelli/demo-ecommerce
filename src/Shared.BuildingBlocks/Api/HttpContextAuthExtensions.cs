using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Shared.BuildingBlocks.Api;

public static class HttpContextAuthExtensions
{
    private const string AnonymousIdHeaderName = "X-Anonymous-Id";

    public static bool TryGetAuthenticatedUserId(this HttpContext context, out Guid userId)
    {
        userId = Guid.Empty;
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var raw = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        return Guid.TryParse(raw, out userId);
    }

    public static Guid GetRequiredUserId(this HttpContext context)
    {
        var raw = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Missing subject claim.");

        if (!Guid.TryParse(raw, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid subject claim.");
        }

        return userId;
    }

    public static Guid GetRequiredAnonymousId(this HttpContext context)
    {
        var raw = context.Request.Headers[AnonymousIdHeaderName].ToString();
        if (!Guid.TryParse(raw, out var anonymousId))
        {
            throw new UnauthorizedAccessException("Missing or invalid anonymous identity.");
        }

        return anonymousId;
    }

    public static Guid ResolveActorId(this HttpContext context)
    {
        return context.TryGetAuthenticatedUserId(out var authenticatedUserId)
            ? authenticatedUserId
            : context.GetRequiredAnonymousId();
    }

    public static string GetRequiredBearerToken(this HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";

        if (!header.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Missing bearer token.");
        }

        var token = header[bearerPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException("Missing bearer token.");
        }

        return token;
    }
}
