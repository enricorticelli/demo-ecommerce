using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions.Idempotency;
using Payment.Application.Abstractions.Repositories;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Messaging;

namespace Payment.Application.Handlers;

public sealed class AuthorizePaymentOnOrderCreatedHandler(
    IPaymentSessionRepository paymentSessionRepository,
    IPaymentEventDeduplicationStore deduplicationStore,
    ILogger<AuthorizePaymentOnOrderCreatedHandler> logger)
    : IntegrationEventHandlerBase<OrderCreatedV1>(deduplicationStore, logger)
{
    public Task Handle(OrderCreatedV1 orderCreatedEvent, CancellationToken cancellationToken)
    {
        return HandleDeduplicatedAsync(
            orderCreatedEvent,
            async ct =>
            {
                var session = await paymentSessionRepository.GetByOrderIdAsync(orderCreatedEvent.OrderId, ct);
                if (session is null)
                {
                    session = Domain.Entities.PaymentSession.Create(
                        orderCreatedEvent.OrderId,
                        orderCreatedEvent.UserId,
                        orderCreatedEvent.TotalAmount,
                        orderCreatedEvent.PaymentMethod,
                        string.Empty);
                    paymentSessionRepository.Add(session);
                }
                else
                {
                    _ = session.UpdateCheckoutContext(
                        orderCreatedEvent.UserId,
                        orderCreatedEvent.TotalAmount,
                        orderCreatedEvent.PaymentMethod);
                }

                await paymentSessionRepository.SaveChangesAsync(ct);
            },
            cancellationToken);
    }
}
