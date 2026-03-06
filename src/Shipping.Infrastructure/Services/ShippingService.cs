using Marten;
using Shared.BuildingBlocks.Contracts;
using Shipping.Application;
using Shipping.Domain;
using Wolverine;

namespace Shipping.Infrastructure;

public sealed class ShippingService(IDocumentSession documentSession, IMessageBus bus) : IShippingService
{
    public async Task<ShipmentResult> CreateShipmentAsync(ShippingCreateRequestedV1 request, CancellationToken cancellationToken)
    {
        var trackingCode = $"TRK-{Guid.NewGuid():N}"[..16];
        documentSession.Store(new ShipmentAggregate
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            UserId = request.UserId,
            TrackingCode = trackingCode,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await documentSession.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new ShippingCreatedV1(request.OrderId, trackingCode));

        return new ShipmentResult(request.OrderId, trackingCode);
    }
}
