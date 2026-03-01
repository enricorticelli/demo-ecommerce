using Marten;
using Shared.BuildingBlocks.Contracts;
using Shipping.Api.Domain;
using Wolverine;

namespace Shipping.Api.Handlers;

public class ShippingHandlers
{
    public static async Task Handle(ShippingCreateRequestedV1 message, IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var trackingCode = $"TRK-{Guid.NewGuid():N}"[..16];
        session.Store(new ShipmentDocument
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            UserId = message.UserId,
            TrackingCode = trackingCode,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new ShippingCreatedV1(message.OrderId, trackingCode));
    }
}
