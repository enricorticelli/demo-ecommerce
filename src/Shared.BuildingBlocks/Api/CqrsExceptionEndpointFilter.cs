using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Shared.BuildingBlocks.Api;

public sealed class CqrsExceptionEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.Problem(new ProblemDetails
            {
                Title = "Invalid request",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Type = "https://httpstatuses.com/400"
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Problem(new ProblemDetails
            {
                Title = "Invalid operation",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Type = "https://httpstatuses.com/400"
            });
        }
    }
}
