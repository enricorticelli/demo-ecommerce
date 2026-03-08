using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Idempotency;
using Order.Application.Abstractions.Repositories;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Warehouse;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Messaging;

namespace Order.Application.Handlers;

public sealed class HandleStockRejectedOnOrderHandler(
    IOrderRepository orderRepository,
    IOrderEventDeduplicationStore deduplicationStore,
    IDomainEventPublisher eventPublisher,
    ILogger<HandleStockRejectedOnOrderHandler> logger)
    : IntegrationEventHandlerBase<StockRejectedV1>(deduplicationStore, logger)
{
    public Task Handle(StockRejectedV1 integrationEvent, CancellationToken cancellationToken)
    {
        return HandleDeduplicatedAsync(
            integrationEvent,
            async ct =>
            {
                var order = await orderRepository.GetByIdAsync(integrationEvent.OrderId, ct)
                    ?? throw new NotFoundAppException($"Order '{integrationEvent.OrderId}' not found.");

                var initialStatus = order.Status.ToString();
                var wasCancelled = order.ApplyStockRejected(integrationEvent.Reason);
                var finalStatus = order.Status.ToString();

                logger.LogInformation(
                    "Applied StockRejectedV1 on order. orderId={OrderId} wasCancelled={WasCancelled} statusBefore={StatusBefore} statusAfter={StatusAfter}",
                    order.Id,
                    wasCancelled,
                    initialStatus,
                    finalStatus);

                if (wasCancelled)
                {
                    var cancelledEvent = new OrderCancelledV1(
                        order.Id,
                        integrationEvent.Reason,
                        CreateMetadata(integrationEvent.Metadata.CorrelationId, "Order"));

                    await eventPublisher.PublishAndFlushAsync(cancelledEvent, ct);
                }

                await orderRepository.SaveChangesAsync(ct);
            },
            cancellationToken);
    }
}
