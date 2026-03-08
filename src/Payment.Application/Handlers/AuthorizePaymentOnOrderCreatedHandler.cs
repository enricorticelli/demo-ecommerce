using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions.Idempotency;
using Payment.Application.Abstractions.Services;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Messaging;

namespace Payment.Application.Handlers;

public sealed class AuthorizePaymentOnOrderCreatedHandler(
    IPaymentAuthorizationService paymentAuthorizationService,
    IPaymentEventDeduplicationStore deduplicationStore,
    IDomainEventPublisher eventPublisher,
    ILogger<AuthorizePaymentOnOrderCreatedHandler> logger)
    : IntegrationEventHandlerBase<OrderCreatedV1>(deduplicationStore, logger)
{
    public Task Handle(OrderCreatedV1 orderCreatedEvent, CancellationToken cancellationToken)
    {
        return HandleDeduplicatedAsync(
            orderCreatedEvent,
            async ct =>
            {
                var result = await paymentAuthorizationService.AuthorizeAsync(orderCreatedEvent, ct);

                IntegrationEventBase integrationEvent = result.IsAuthorized
                    ? new PaymentAuthorizedV1(
                        result.OrderId,
                        result.TransactionId ?? string.Empty,
                        CreateMetadata(orderCreatedEvent.Metadata.CorrelationId, "Payment"))
                    : new PaymentRejectedV1(
                        result.OrderId,
                        result.FailureReason ?? "Payment authorization rejected.",
                        CreateMetadata(orderCreatedEvent.Metadata.CorrelationId, "Payment"));

                await eventPublisher.PublishAndFlushAsync(integrationEvent, ct);
            },
            cancellationToken);
    }
}
