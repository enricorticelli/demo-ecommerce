using Shipping.Api.Contracts;
using Shipping.Api.Contracts.Requests;
using Shipping.Api.Contracts.Responses;
using Shipping.Api.Mappers;
using Shipping.Application.Abstractions.Commands;
using Shipping.Application.Abstractions.Queries;
using Shared.BuildingBlocks.Api.Errors;

namespace Shipping.Api.Endpoints;

public static class ShippingEndpoints
{
    public static RouteGroupBuilder MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ShippingRoutes.Base)
            .WithTags("Shipping");

        group.MapPost("/", CreateShipment)
            .WithName("CreateShipment");
        group.MapGet("/", ListShipments)
            .WithName("ListShipments");
        group.MapGet("/orders/{orderId:guid}", GetShipmentByOrder)
            .WithName("GetShipmentByOrder");
        group.MapPost("/{shipmentId:guid}/status", UpdateShipmentStatus)
            .WithName("UpdateShipmentStatus");
        return group;
    }

    private static async Task<IResult> CreateShipment(
        CreateShipmentRequest request,
        IShippingCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await service.CreateAsync(request.ToCreateCommand(), cancellationToken);
            var response = new CreateShipmentResponse(shipment.OrderId, shipment.TrackingCode);
            return Results.Created($"{ShippingRoutes.Base}/orders/{shipment.OrderId}", response);
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> ListShipments(
        IShippingQueryService service,
        int? limit,
        int? offset,
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        var normalizedLimit = Math.Clamp(limit ?? 50, 1, 200);
        var normalizedOffset = Math.Max(offset ?? 0, 0);

        var shipments = await service.ListAsync(normalizedLimit, normalizedOffset, searchTerm, cancellationToken);
        return Results.Ok(shipments.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> GetShipmentByOrder(
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
}
