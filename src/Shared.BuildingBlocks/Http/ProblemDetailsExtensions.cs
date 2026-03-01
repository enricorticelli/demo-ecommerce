using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.BuildingBlocks.Http;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddDefaultProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var correlationId = context.HttpContext.Items.TryGetValue(CorrelationId.ItemKey, out var value)
                    ? value?.ToString()
                    : context.HttpContext.TraceIdentifier;

                context.ProblemDetails.Extensions["correlationId"] = correlationId;
            };
        });

        return services;
    }

    public static IResult ValidationProblem(Dictionary<string, string[]> errors, string detail = "Request validation failed")
    {
        return TypedResults.Problem(new ProblemDetails
        {
            Title = "Validation Error",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail,
            Type = "https://httpstatuses.com/400",
            Extensions = { ["errors"] = errors }
        });
    }
}
