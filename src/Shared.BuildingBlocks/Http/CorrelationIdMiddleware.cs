using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Shared.BuildingBlocks.Http;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";
}

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(CorrelationId.HeaderName, out var existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[CorrelationId.ItemKey] = correlationId;
        context.Response.Headers[CorrelationId.HeaderName] = correlationId;
        await next(context);
    }
}

public static class CorrelationIdExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) => app.UseMiddleware<CorrelationIdMiddleware>();
}
