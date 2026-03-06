using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Http;

namespace Shared.BuildingBlocks.Api;

public static class DefaultApiExtensions
{
    public static IServiceCollection AddDefaultApiServices(this IServiceCollection services)
    {
        services.AddDefaultProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddHealthChecks();
        services.AddScoped<CqrsExceptionEndpointFilter>();
        services.AddCors(options =>
        {
            options.AddPolicy("default", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        return services;
    }

    public static WebApplication UseDefaultApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseCors("default");
        app.UseCorrelationId();

        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");

        return app;
    }
}
