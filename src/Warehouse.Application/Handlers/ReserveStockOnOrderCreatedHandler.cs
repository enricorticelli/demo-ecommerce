using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Warehouse;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Messaging;
using Warehouse.Application.Abstractions.Idempotency;
using Warehouse.Application.Abstractions.Services;

namespace Warehouse.Application.Handlers;

public sealed class ReserveStockOnOrderCreatedHandler(
    IStockReservationService stockReservationService,
    IWarehouseEventDeduplicationStore deduplicationStore,
    IDomainEventPublisher eventPublisher,
    ILogger<ReserveStockOnOrderCreatedHandler> logger)
    : IntegrationEventHandlerBase<OrderCreatedV1>(deduplicationStore, logger)
{
    public Task Handle(OrderCreatedV1 orderCreatedEvent, CancellationToken cancellationToken)
    {
        return HandleDeduplicatedAsync(
            orderCreatedEvent,
            async ct =>
            {
                var result = await stockReservationService.ReserveAsync(orderCreatedEvent, ct);

                IntegrationEventBase integrationEvent = result.IsReserved
                    ? new StockReservedV1(
                        result.OrderId,
                        CreateMetadata(orderCreatedEvent.Metadata.CorrelationId, "Warehouse"))
                    : new StockRejectedV1(
                        result.OrderId,
                        result.FailureReason ?? "Stock reservation failed.",
                        CreateMetadata(orderCreatedEvent.Metadata.CorrelationId, "Warehouse"));

                await eventPublisher.PublishAndFlushAsync(integrationEvent, ct);
            },
            cancellationToken);
    }
}
