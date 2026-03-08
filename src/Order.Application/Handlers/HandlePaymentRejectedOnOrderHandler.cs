using Order.Application.Abstractions.Idempotency;
using Order.Application.Abstractions.Repositories;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Exceptions;

namespace Order.Application.Handlers;

public sealed class HandlePaymentRejectedOnOrderHandler(
    IOrderRepository orderRepository,
    IOrderEventDeduplicationStore deduplicationStore,
    IDomainEventPublisher eventPublisher)
{
    public async Task HandleAsync(PaymentRejectedV1 integrationEvent, CancellationToken cancellationToken)
    {
        if (await deduplicationStore.HasProcessedAsync(integrationEvent.Metadata.EventId, cancellationToken))
        {
            return;
        }

        var order = await orderRepository.GetByIdAsync(integrationEvent.OrderId, cancellationToken)
            ?? throw new NotFoundAppException($"Order '{integrationEvent.OrderId}' not found.");

        var wasCancelled = order.ApplyPaymentRejected(integrationEvent.Reason);

        if (wasCancelled)
        {
            var cancelledEvent = new OrderCancelledV1(
                order.Id,
                integrationEvent.Reason,
                CreateMetadata(integrationEvent.Metadata.CorrelationId));

            await eventPublisher.PublishAndFlushAsync(cancelledEvent, cancellationToken);
        }

        await orderRepository.SaveChangesAsync(cancellationToken);
        await deduplicationStore.MarkProcessedAsync(integrationEvent.Metadata.EventId, cancellationToken);
    }

    private static IntegrationEventMetadata CreateMetadata(string correlationId)
    {
        return new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, correlationId, "Order");
    }
}
