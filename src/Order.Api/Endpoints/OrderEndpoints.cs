using Microsoft.AspNetCore.Http.HttpResults;
using Order.Application;
using Order.Api.Contracts;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Cqrs;

namespace Order.Api.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(OrderRoutes.Base)
            .WithTags("Order")
            .AddEndpointFilter<CqrsExceptionEndpointFilter>();

        group.MapPost("/", CreateOrder)
            .WithName("CreateOrder");

        group.MapGet("/", ListOrders)
            .WithName("ListOrders");

        group.MapGet("/{orderId:guid}", GetOrder)
            .WithName("GetOrder");

        return group;
    }

    private static async Task<IResult> CreateOrder(
        CreateOrderCommand command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await commandDispatcher.ExecuteAsync(command, cancellationToken);
            if (result is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Accepted($"{OrderRoutes.Base}/{result.OrderId}", new { orderId = result.OrderId, status = result.Status });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Problem(
                title: "Invalid cart",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<Results<Ok<object>, NotFound>> GetOrder(Guid orderId, IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var order = await queryDispatcher.ExecuteAsync(new GetOrderByIdQuery(orderId), cancellationToken);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok((object)order);
    }

    private static async Task<Ok<IReadOnlyList<OrderView>>> ListOrders(
        IQueryDispatcher queryDispatcher,
        int? limit,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit ?? 50, 1, 200);
        var orders = await queryDispatcher.ExecuteAsync(new GetOrdersQuery(safeLimit), cancellationToken);
        return TypedResults.Ok(orders);
    }
}
