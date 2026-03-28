using Microsoft.AspNetCore.Http;

namespace Shared.BuildingBlocks.Api;

public static class HttpContextAuthExtensions
{
    private const string AnonymousIdHeaderName = "X-Anonymous-Id";

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
        return context.GetRequiredAnonymousId();
    }
}
