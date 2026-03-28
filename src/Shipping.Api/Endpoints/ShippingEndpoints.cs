using Shipping.Api.Contracts;
using Shipping.Api.Mappers;
using Shipping.Application.Abstractions.Queries;
using Shipping.Application.Views;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Api.Errors;
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
        return storeGroup;
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

    private static void EnsureShipmentOwnership(ShipmentView shipment, Guid authenticatedUserId)
    {
        if (shipment.UserId != authenticatedUserId)
        {
            throw new ForbiddenAppException("The shipment does not belong to the authenticated user.");
        }
    }
}
