using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
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
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "E-commerce API",
                Version = "v1"
            });
        });
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
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "openapi/{documentName}.json";
        });

        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");

        return app;
    }
}
