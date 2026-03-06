using Microsoft.AspNetCore.Builder;

namespace Shared.BuildingBlocks.Http;

public static class CorrelationIdExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) => app.UseMiddleware<CorrelationIdMiddleware>();
}
