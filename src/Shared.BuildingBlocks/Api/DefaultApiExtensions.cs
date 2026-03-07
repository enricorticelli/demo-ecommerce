using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Observability;

namespace Shared.BuildingBlocks.Api;

public static class DefaultApiExtensions
{
    public static WebApplicationBuilder AddDefaultApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDefaultApiServices();
        builder.AddObservability();
        return builder;
    }

    public static IServiceCollection AddDefaultApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddProblemDetails();
        services.AddHealthChecks();
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

        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");

        return app;
    }
}
