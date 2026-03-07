using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Order.Application;
using Order.Application.Abstractions;
using Order.Api.Contracts;
using Order.Application.Queries;
using Order.Application.Views;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Cqrs;
using Shared.BuildingBlocks.Cqrs.Abstractions;

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
        group.MapPost("/{orderId:guid}/manual-complete", ManualCompleteOrder)
            .WithName("ManualCompleteOrder");
        group.MapPost("/{orderId:guid}/manual-cancel", ManualCancelOrder)
            .WithName("ManualCancelOrder");

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
                title: "Invalid order request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<Results<Ok<object>, NotFound>> GetOrder(
        Guid orderId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken,
        [FromQuery(Name = "includeNonCompleted")] bool includeNonCompleted = false)
    {
        var order = await queryDispatcher.ExecuteAsync(new GetOrderByIdQuery(orderId), cancellationToken);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        if (!includeNonCompleted && !string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok((object)order);
    }

    private static async Task<Ok<IReadOnlyList<OrderView>>> ListOrders(
        IQueryDispatcher queryDispatcher,
        int? limit,
        int? offset,
        CancellationToken cancellationToken,
        [FromQuery(Name = "includeNonCompleted")] bool includeNonCompleted = false)
    {
        var safeLimit = Math.Clamp(limit ?? 50, 1, 200);
        var safeOffset = Math.Max(offset ?? 0, 0);
        var orders = await queryDispatcher.ExecuteAsync(new GetOrdersQuery(safeLimit, safeOffset), cancellationToken);
        var filteredOrders = includeNonCompleted
            ? orders
            : orders.Where(order => string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase)).ToList();

        return TypedResults.Ok(filteredOrders);
    }

    private static async Task<IResult> ManualCompleteOrder(
        Guid orderId,
        ManualCompleteOrderRequest request,
        IOrderStateReader orderStateReader,
        IOrderStateStore orderStateStore,
        IOrderEventPublisher orderEventPublisher,
        CancellationToken cancellationToken)
    {
        var orderState = await orderStateReader.GetAsync(orderId, cancellationToken);
        if (orderState is null)
        {
            return TypedResults.NotFound();
        }

        if (orderState.Status is Order.Domain.Enums.OrderStatus.Completed or Order.Domain.Enums.OrderStatus.Failed)
        {
            return TypedResults.Problem(
                title: "Order in terminal state",
                detail: "Order cannot be manually completed because it is already terminal.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var trackingCode = string.IsNullOrWhiteSpace(request.TrackingCode)
            ? $"MAN-TRK-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"
            : request.TrackingCode.Trim();

        var transactionId = string.IsNullOrWhiteSpace(request.TransactionId)
            ? (string.IsNullOrWhiteSpace(orderState.TransactionId)
                ? $"MAN-TX-{Guid.NewGuid():N}"
                : orderState.TransactionId)
            : request.TransactionId.Trim();

        await orderStateStore.MarkCompletedAsync(orderId, trackingCode, transactionId, cancellationToken);
        await orderEventPublisher.PublishOrderCompletedAsync(orderId, orderState.CartId, orderState.UserId, trackingCode, transactionId);

        return TypedResults.Ok(new
        {
            orderId,
            status = "Completed",
            trackingCode,
            transactionId,
            mode = "manual"
        });
    }

    private static async Task<IResult> ManualCancelOrder(
        Guid orderId,
        ManualCancelOrderRequest request,
        IOrderStateReader orderStateReader,
        IOrderStateStore orderStateStore,
        IOrderEventPublisher orderEventPublisher,
        CancellationToken cancellationToken)
    {
        var orderState = await orderStateReader.GetAsync(orderId, cancellationToken);
        if (orderState is null)
        {
            return TypedResults.NotFound();
        }

        if (orderState.Status is Order.Domain.Enums.OrderStatus.Failed)
        {
            return TypedResults.Problem(
                title: "Order in terminal state",
            detail: "Order cannot be manually cancelled because it is already failed.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Cancelled by backoffice"
            : request.Reason.Trim();

        await orderStateStore.MarkFailedAsync(orderId, reason, cancellationToken);
        await orderEventPublisher.PublishOrderFailedAsync(orderId, reason);

        return TypedResults.Ok(new
        {
            orderId,
            status = "Failed",
            reason,
            mode = "manual"
        });
    }
}
