using Order.Api.Contracts;
using Order.Api.Contracts.Requests;
using Order.Api.Contracts.Responses;
using Order.Api.Mappers;
using Order.Application.Abstractions.Commands;
using Order.Application.Abstractions.Queries;
using Order.Application.Commands;
using Shared.BuildingBlocks.Api.Correlation;
using Shared.BuildingBlocks.Api.Errors;

namespace Order.Api.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(OrderRoutes.Base)
            .WithTags("Order");

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
        CreateOrderRequest request,
        IOrderCommandService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var order = await service.CreateAsync(request.ToCreateCommand(correlationId), cancellationToken);
            return Results.Created($"{OrderRoutes.Base}/{order.Id}", new OrderCreatedResponse(order.Id, order.Status));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> ListOrders(
        IOrderQueryService service,
        int? limit,
        int? offset,
        bool includeNonCompleted,
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        var normalizedLimit = Math.Clamp(limit ?? 50, 1, 200);
        var normalizedOffset = Math.Max(offset ?? 0, 0);

        var orders = (await service.ListAsync(cancellationToken))
            .Select(x => x.ToResponse());

        if (!includeNonCompleted)
        {
            orders = orders.Where(x => IsTerminalStatus(x.Status));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var token = searchTerm.Trim();
            orders = orders.Where(x => MatchesSearch(x, token));
        }

        var page = orders
            .Skip(normalizedOffset)
            .Take(normalizedLimit)
            .ToArray();

        return Results.Ok(page);
    }

    private static async Task<IResult> GetOrder(
        Guid orderId,
        IOrderQueryService service,
        bool includeNonCompleted,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await service.GetByIdAsync(orderId, cancellationToken);
            var response = order.ToResponse();

            if (!includeNonCompleted && !IsTerminalStatus(response.Status))
            {
                return Results.NotFound();
            }

            return Results.Ok(response);
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> ManualCompleteOrder(
        Guid orderId,
        ManualCompleteOrderRequest request,
        IOrderCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await service.ManualCompleteAsync(new ManualCompleteOrderCommand(orderId, request.TrackingCode, request.TransactionId), cancellationToken);
            return Results.Ok(new ManualCompleteOrderResponse(order.Id, order.Status, order.TrackingCode, order.TransactionId, "manual"));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> ManualCancelOrder(
        Guid orderId,
        ManualCancelOrderRequest request,
        IOrderCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await service.ManualCancelAsync(new ManualCancelOrderCommand(orderId, request.Reason), cancellationToken);
            return Results.Ok(new ManualCancelOrderResponse(order.Id, "Failed", order.FailureReason, "manual"));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static bool IsTerminalStatus(string status)
    {
        return string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesSearch(OrderResponse response, string searchToken)
    {
        return response.Id.ToString().Contains(searchToken, StringComparison.OrdinalIgnoreCase)
            || response.CartId.ToString().Contains(searchToken, StringComparison.OrdinalIgnoreCase)
            || response.UserId.ToString().Contains(searchToken, StringComparison.OrdinalIgnoreCase)
            || response.Status.Contains(searchToken, StringComparison.OrdinalIgnoreCase)
            || response.PaymentMethod.Contains(searchToken, StringComparison.OrdinalIgnoreCase)
            || response.IdentityType.Contains(searchToken, StringComparison.OrdinalIgnoreCase)
            || (response.TrackingCode?.Contains(searchToken, StringComparison.OrdinalIgnoreCase) ?? false)
            || (response.TransactionId?.Contains(searchToken, StringComparison.OrdinalIgnoreCase) ?? false)
            || (response.FailureReason?.Contains(searchToken, StringComparison.OrdinalIgnoreCase) ?? false)
            || (response.AuthenticatedUserId?.ToString().Contains(searchToken, StringComparison.OrdinalIgnoreCase) ?? false)
            || (response.AnonymousId?.ToString().Contains(searchToken, StringComparison.OrdinalIgnoreCase) ?? false)
            || response.Customer.FirstName.Contains(searchToken, StringComparison.OrdinalIgnoreCase)
            || response.Customer.LastName.Contains(searchToken, StringComparison.OrdinalIgnoreCase)
            || response.Customer.Email.Contains(searchToken, StringComparison.OrdinalIgnoreCase);
    }
}
