using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shipping.Application.Abstractions.Commands;
using Shipping.Application.Abstractions.Idempotency;
using Shipping.Application.Commands;

namespace Shipping.Application.Handlers;

public sealed class CreateShipmentOnOrderCompletedHandler(
    IShippingCommandService shippingCommandService,
    IShippingEventDeduplicationStore deduplicationStore)
{
    public async Task HandleAsync(OrderCompletedV1 integrationEvent, CancellationToken cancellationToken)
    {
        if (await deduplicationStore.HasProcessedAsync(integrationEvent.Metadata.EventId, cancellationToken))
        {
            return;
        }

        await shippingCommandService.CreateAsync(
            new CreateShipmentCommand(integrationEvent.OrderId, integrationEvent.UserId),
            cancellationToken);

        await deduplicationStore.MarkProcessedAsync(integrationEvent.Metadata.EventId, cancellationToken);
    }
}
