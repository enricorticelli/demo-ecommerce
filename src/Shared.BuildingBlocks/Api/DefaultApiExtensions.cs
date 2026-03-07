using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.BuildingBlocks.Api;

public static class DefaultApiExtensions
{
    public static IServiceCollection AddDefaultApiServices(this IServiceCollection services)
    {
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

        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");

        return app;
    }
}
