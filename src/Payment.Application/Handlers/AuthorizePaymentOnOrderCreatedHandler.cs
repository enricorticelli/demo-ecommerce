using Payment.Application.Abstractions.Idempotency;
using Payment.Application.Abstractions.Services;
using Payment.Application.Services;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Shared.BuildingBlocks.Contracts.Messaging;

namespace Payment.Application.Handlers;

public sealed class AuthorizePaymentOnOrderCreatedHandler(
    IPaymentAuthorizationService paymentAuthorizationService,
    IPaymentEventDeduplicationStore deduplicationStore,
    IDomainEventPublisher eventPublisher)
{
    public async Task HandleAsync(OrderCreatedV1 orderCreatedEvent, CancellationToken cancellationToken)
    {
        if (await deduplicationStore.HasProcessedAsync(orderCreatedEvent.Metadata.EventId, cancellationToken))
        {
            return;
        }

        var result = await paymentAuthorizationService.AuthorizeAsync(orderCreatedEvent, cancellationToken);

        IntegrationEventBase integrationEvent = result.IsAuthorized
            ? new PaymentAuthorizedV1(
                result.OrderId,
                result.TransactionId ?? string.Empty,
                CreateMetadata(orderCreatedEvent.Metadata.CorrelationId))
            : new PaymentRejectedV1(
                result.OrderId,
                result.FailureReason ?? "Payment authorization rejected.",
                CreateMetadata(orderCreatedEvent.Metadata.CorrelationId));

        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);
        await deduplicationStore.MarkProcessedAsync(orderCreatedEvent.Metadata.EventId, cancellationToken);
    }

    private static IntegrationEventMetadata CreateMetadata(string correlationId)
    {
        return new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, correlationId, "Payment");
    }
}
