using Marten;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Contracts;
using Shipping.Api.Domain;
using Wolverine;

namespace Shipping.Api.Endpoints;

public static class ShippingEndpoints
{
    public static RouteGroupBuilder MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/shipments")
            .WithTags("Shipping");

        group.MapPost("/", CreateShipment)
            .WithName("CreateShipment");

        return group;
    }

    private static async Task<Ok<object>> CreateShipment(
        ShippingCreateRequestedV1 request,
        IDocumentSession session,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var trackingCode = $"TRK-{Guid.NewGuid():N}"[..16];
        session.Store(new ShipmentDocument
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            UserId = request.UserId,
            TrackingCode = trackingCode,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new ShippingCreatedV1(request.OrderId, trackingCode));

        return TypedResults.Ok((object)new { request.OrderId, TrackingCode = trackingCode });
    }
}
