using Order.Api.Contracts;
using Order.Api.Contracts.Requests;
using Order.Api.Contracts.Responses;
using Order.Api.Mappers;
using Order.Application.Abstractions.Commands;
using Order.Application.Abstractions.Queries;
using Order.Application.Commands;
using Order.Application.Views;
using Shared.BuildingBlocks.Api.Correlation;
using Shared.BuildingBlocks.Api.Errors;
using Shared.BuildingBlocks.Api.Pagination;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Exceptions;

namespace Order.Api.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var storeGroup = app.MapGroup(OrderRoutes.StoreBase)
            .WithTags("Order");

        storeGroup.MapPost("/", CreateOrder)
            .WithName("StoreCreateOrder");
        storeGroup.MapGet("/", StoreListOrders)
            .WithName("StoreListOrders");
        storeGroup.MapGet("/{orderId:guid}", GetOrder)
            .WithName("StoreGetOrder");
        storeGroup.MapPost("/{orderId:guid}/manual-cancel", StoreManualCancelOrder)
            .WithName("StoreManualCancelOrder");
        storeGroup.MapPost("/claim-guest", ClaimGuestOrders)
            .RequireAuthorization("CustomerPolicy")
            .WithName("StoreClaimGuestOrders");

        var internalGroup = app.MapGroup(OrderRoutes.InternalBase)
            .WithTags("OrderInternal");

        internalGroup.MapPost("/claim-guest", InternalClaimGuestOrders)
            .WithName("InternalClaimGuestOrders");

        var adminGroup = app.MapGroup(OrderRoutes.AdminBase)
            .WithTags("Order")
            .RequireAuthorization("AdminPolicy");

        adminGroup.MapGet("/", ListOrders)
            .WithName("AdminListOrders");
        adminGroup.MapGet("/{orderId:guid}", AdminGetOrder)
            .WithName("AdminGetOrder");
        adminGroup.MapPost("/{orderId:guid}/manual-complete", AdminManualCompleteOrder)
            .WithName("AdminManualCompleteOrder");
        adminGroup.MapPost("/{orderId:guid}/manual-cancel", AdminManualCancelOrder)
            .WithName("AdminManualCancelOrder");

        return adminGroup;
    }

    private static async Task<IResult> CreateOrder(
        CreateOrderRequest request,
        IOrderCommandService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var authenticatedUserId = httpContext.TryGetAuthenticatedUserId(out var userId)
                ? userId
                : (Guid?)null;
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var order = await service.CreateAsync(request.ToCreateCommand(authenticatedUserId, correlationId), cancellationToken);
            return Results.Created($"{OrderRoutes.StoreBase}/{order.Id}", new OrderCreatedResponse(order.Id, order.Status));
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
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        var (normalizedLimit, normalizedOffset) = PaginationNormalizer.Normalize(limit, offset);

        var orders = (await service.ListAsync(cancellationToken))
            .Select(x => x.ToResponse());

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

    private static async Task<IResult> StoreListOrders(
        HttpContext context,
        IOrderQueryService service,
        Guid? anonymousId,
        int? limit,
        int? offset,
        CancellationToken cancellationToken)
    {
        var (normalizedLimit, normalizedOffset) = PaginationNormalizer.Normalize(limit, offset);
        var source = context.TryGetAuthenticatedUserId(out var authenticatedUserId)
            ? await service.ListByAuthenticatedUserIdAsync(authenticatedUserId, cancellationToken)
            : (await service.ListAsync(cancellationToken))
                .Where(x => x.AnonymousId.HasValue && x.AnonymousId.Value == (anonymousId ?? context.GetRequiredAnonymousId()))
                .ToArray();

        var page = source
            .Skip(normalizedOffset)
            .Take(normalizedLimit)
            .Select(x => x.ToResponse())
            .ToArray();

        return Results.Ok(page);
    }

    private static async Task<IResult> GetOrder(
        HttpContext context,
        Guid orderId,
        Guid? anonymousId,
        IOrderQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorAnonymousId = anonymousId;
            var order = await service.GetByIdAsync(orderId, cancellationToken);
            EnsureOrderOwnership(order, context, ref actorAnonymousId);
            var response = order.ToResponse();

            return Results.Ok(response);
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> AdminGetOrder(
        Guid orderId,
        IOrderQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await service.GetByIdAsync(orderId, cancellationToken);
            return Results.Ok(order.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> AdminManualCompleteOrder(
        Guid orderId,
        ManualCompleteOrderRequest request,
        IOrderCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await service.AdminManualCompleteAsync(new ManualCompleteOrderCommand(orderId, request.TrackingCode, request.TransactionId), cancellationToken);
            return Results.Ok(new ManualCompleteOrderResponse(order.Id, order.Status, order.TrackingCode, order.TransactionId, "manual"));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> AdminManualCancelOrder(
        Guid orderId,
        ManualCancelOrderRequest request,
        IOrderCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await service.AdminManualCancelAsync(new ManualCancelOrderCommand(orderId, request.Reason), cancellationToken);
            return Results.Ok(new ManualCancelOrderResponse(order.Id, order.Status, order.FailureReason, "manual"));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> StoreManualCancelOrder(
        HttpContext context,
        Guid orderId,
        ManualCancelOrderRequest request,
        Guid? anonymousId,
        IOrderQueryService queryService,
        IOrderCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorAnonymousId = anonymousId;
            var existingOrder = await queryService.GetByIdAsync(orderId, cancellationToken);
            EnsureOrderOwnership(existingOrder, context, ref actorAnonymousId);

            var order = await service.StoreManualCancelAsync(new ManualCancelOrderCommand(orderId, request.Reason), cancellationToken);
            return Results.Ok(new ManualCancelOrderResponse(order.Id, order.Status, order.FailureReason, "manual"));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> ClaimGuestOrders(
        HttpContext context,
        ClaimGuestOrdersRequest request,
        IOrderCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var authenticatedUserId = context.GetRequiredUserId();
            var claimedCount = await service.ClaimGuestOrdersAsync(
                new ClaimGuestOrdersCommand(authenticatedUserId, request.CustomerEmail),
                cancellationToken);

            return Results.Ok(new ClaimGuestOrdersResponse(claimedCount));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> InternalClaimGuestOrders(
        HttpContext context,
        InternalClaimGuestOrdersRequest request,
        IConfiguration configuration,
        IOrderCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var configuredApiKey = configuration["Order:InternalApiKey"] ?? "dev-order-internal-key-change-me";
            var providedApiKey = context.Request.Headers["X-Internal-Api-Key"].ToString();
            if (!string.Equals(configuredApiKey, providedApiKey, StringComparison.Ordinal))
            {
                throw new ForbiddenAppException("Invalid internal API key.");
            }

            var claimedCount = await service.ClaimGuestOrdersAsync(
                new ClaimGuestOrdersCommand(request.AuthenticatedUserId, request.CustomerEmail),
                cancellationToken);

            return Results.Ok(new ClaimGuestOrdersResponse(claimedCount));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static void EnsureOrderOwnership(OrderView order, HttpContext context, ref Guid? anonymousId)
    {
        if (context.TryGetAuthenticatedUserId(out var authenticatedUserId))
        {
            if (!order.AuthenticatedUserId.HasValue || order.AuthenticatedUserId.Value != authenticatedUserId)
            {
                throw new ForbiddenAppException("The order does not belong to the authenticated user.");
            }

            return;
        }

        anonymousId ??= context.GetRequiredAnonymousId();
        if (!order.AnonymousId.HasValue || order.AnonymousId.Value != anonymousId.Value)
        {
            throw new ForbiddenAppException("The order does not belong to the anonymous user.");
        }
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
