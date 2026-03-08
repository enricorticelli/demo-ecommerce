using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Idempotency;
using Order.Application.Abstractions.Repositories;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Messaging;

namespace Order.Application.Handlers;

public sealed class HandlePaymentAuthorizedOnOrderHandler(
    IOrderRepository orderRepository,
    IOrderEventDeduplicationStore deduplicationStore,
    IDomainEventPublisher eventPublisher,
    ILogger<HandlePaymentAuthorizedOnOrderHandler> logger)
    : IntegrationEventHandlerBase<PaymentAuthorizedV1>(deduplicationStore, logger)
{
    public Task Handle(PaymentAuthorizedV1 integrationEvent, CancellationToken cancellationToken)
    {
        return HandleDeduplicatedAsync(
            integrationEvent,
            async ct =>
            {
                var order = await orderRepository.GetByIdAsync(integrationEvent.OrderId, ct)
                    ?? throw new NotFoundAppException($"Order '{integrationEvent.OrderId}' not found.");

                var initialStatus = order.Status.ToString();
                var stateChanged = order.ApplyPaymentAuthorized(integrationEvent.TransactionId);
                var finalStatus = order.Status.ToString();

                logger.LogInformation(
                    "Applied PaymentAuthorizedV1 on order. orderId={OrderId} stateChanged={StateChanged} statusBefore={StatusBefore} statusAfter={StatusAfter}",
                    order.Id,
                    stateChanged,
                    initialStatus,
                    finalStatus);

                if (stateChanged && string.Equals(finalStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    var completedEvent = new OrderCompletedV1(
                        order.Id,
                        order.UserId,
                        order.TrackingCode,
                        order.TransactionId,
                        CreateMetadata(integrationEvent.Metadata.CorrelationId, "Order"));

                    await eventPublisher.PublishAndFlushAsync(completedEvent, ct);
                }

                await orderRepository.SaveChangesAsync(ct);
            },
            cancellationToken);
    }
}
