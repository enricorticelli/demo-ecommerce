using Scalar.AspNetCore;

namespace Gateway.Api.Extensions;

public static class GatewayWebApplicationExtensions
{
    public static WebApplication MapGatewayOperationalEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");

        app.MapScalarApiReference("/scalar", options =>
        {
            options.WithTitle("E-commerce Gateway API");
        });

        app.MapGet("/v1/system/info", () => TypedResults.Ok(new
        {
            Name = "ecommerce-gateway",
            Version = "v1",
            Timestamp = DateTimeOffset.UtcNow
        }))
        .WithName("GetSystemInfo")
        .WithTags("System");

        return app;
    }
}