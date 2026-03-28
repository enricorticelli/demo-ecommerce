using Payment.Application.Abstractions.Services;
using Payment.Application.Services;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;

namespace Payment.Infrastructure.Services;

public sealed class InMemoryPaymentAuthorizationService : IPaymentAuthorizationService
{
    public Task<PaymentAuthorizationResult> AuthorizeAsync(OrderCreatedV1 orderCreatedEvent, CancellationToken cancellationToken)
    {
        if (orderCreatedEvent.TotalAmount <= 0)
        {
            return Task.FromResult(new PaymentAuthorizationResult(orderCreatedEvent.OrderId, false, null, "Invalid amount."));
        }

        // Deterministic transaction id keeps behavior predictable for tests and local runs.
        var transactionId = $"TX-{orderCreatedEvent.OrderId:N}";
        return Task.FromResult(new PaymentAuthorizationResult(orderCreatedEvent.OrderId, true, transactionId, null));
    }
}
