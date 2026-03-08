using Order.Application.Abstractions.Idempotency;
using Order.Application.Abstractions.Repositories;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Warehouse;
using Shared.BuildingBlocks.Exceptions;

namespace Order.Application.Handlers;

public sealed class HandleStockReservedOnOrderHandler(
    IOrderRepository orderRepository,
    IOrderEventDeduplicationStore deduplicationStore,
    IDomainEventPublisher eventPublisher)
{
    public async Task HandleAsync(StockReservedV1 integrationEvent, CancellationToken cancellationToken)
    {
        if (await deduplicationStore.HasProcessedAsync(integrationEvent.Metadata.EventId, cancellationToken))
        {
            return;
        }

        var order = await orderRepository.GetByIdAsync(integrationEvent.OrderId, cancellationToken)
            ?? throw new NotFoundAppException($"Order '{integrationEvent.OrderId}' not found.");

        var stateChanged = order.ApplyStockReserved();

        if (stateChanged && string.Equals(order.Status.ToString(), "Completed", StringComparison.OrdinalIgnoreCase))
        {
            var completedEvent = new OrderCompletedV1(
                order.Id,
                order.UserId,
                order.TrackingCode,
                order.TransactionId,
                CreateMetadata(integrationEvent.Metadata.CorrelationId));

            await eventPublisher.PublishAndFlushAsync(completedEvent, cancellationToken);
        }

        await orderRepository.SaveChangesAsync(cancellationToken);
        await deduplicationStore.MarkProcessedAsync(integrationEvent.Metadata.EventId, cancellationToken);
    }

    private static IntegrationEventMetadata CreateMetadata(string correlationId)
    {
        return new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, correlationId, "Order");
    }
}
