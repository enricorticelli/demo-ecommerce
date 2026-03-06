using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.BuildingBlocks.Cqrs;

namespace Shared.BuildingBlocks.Api;

public sealed class CqrsExceptionEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (RequestValidationException ex)
        {
            return TypedResults.Problem(new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request validation failed",
                Type = "https://httpstatuses.com/400",
                Extensions = { ["errors"] = ex.Errors }
            });
        }
    }
}
