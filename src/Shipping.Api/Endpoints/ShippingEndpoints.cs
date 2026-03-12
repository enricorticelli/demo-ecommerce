using Shipping.Api.Contracts;
using Shipping.Api.Contracts.Requests;
using Shipping.Api.Mappers;
using Shipping.Application.Abstractions.Commands;
using Shipping.Application.Abstractions.Queries;
using Shipping.Application.Views;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Api.Errors;
using Shared.BuildingBlocks.Api.Pagination;
using Shared.BuildingBlocks.Exceptions;

namespace Shipping.Api.Endpoints;

public static class ShippingEndpoints
{
    public static RouteGroupBuilder MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        var storeGroup = app.MapGroup(ShippingRoutes.StoreBase)
            .WithTags("Shipping");

        storeGroup.MapGet("/orders/{orderId:guid}", StoreGetShipmentByOrder)
            .WithName("StoreGetShipmentByOrder");

        var adminGroup = app.MapGroup(ShippingRoutes.AdminBase)
            .WithTags("Shipping")
            .RequireAuthorization("AdminPolicy");

        adminGroup.MapGet("/", ListShipments)
            .WithName("AdminListShipments");
        adminGroup.MapGet("/orders/{orderId:guid}", AdminGetShipmentByOrder)
            .WithName("AdminGetShipmentByOrder");
        adminGroup.MapPost("/{shipmentId:guid}/status", UpdateShipmentStatus)
            .WithName("AdminUpdateShipmentStatus");
        return adminGroup;
    }

    private static async Task<IResult> ListShipments(
        IShippingQueryService service,
        int? limit,
        int? offset,
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        var (normalizedLimit, normalizedOffset) = PaginationNormalizer.Normalize(limit, offset);

        var shipments = await service.ListAsync(normalizedLimit, normalizedOffset, searchTerm, cancellationToken);
        return Results.Ok(shipments.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> StoreGetShipmentByOrder(
        HttpContext context,
        Guid orderId,
        IShippingQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = context.ResolveActorId();
            var shipment = await service.GetByOrderIdAsync(orderId, cancellationToken);
            EnsureShipmentOwnership(shipment, actorUserId);
            return Results.Ok(shipment.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> AdminGetShipmentByOrder(
        Guid orderId,
        IShippingQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await service.GetByOrderIdAsync(orderId, cancellationToken);
            return Results.Ok(shipment.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> UpdateShipmentStatus(
        Guid shipmentId,
        UpdateShipmentStatusRequest request,
        IShippingCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await service.UpdateStatusAsync(request.ToUpdateStatusCommand(shipmentId), cancellationToken);
            return Results.Ok(shipment.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static void EnsureShipmentOwnership(ShipmentView shipment, Guid authenticatedUserId)
    {
        if (shipment.UserId != authenticatedUserId)
        {
            throw new ForbiddenAppException("The shipment does not belong to the authenticated user.");
        }
    }
}
