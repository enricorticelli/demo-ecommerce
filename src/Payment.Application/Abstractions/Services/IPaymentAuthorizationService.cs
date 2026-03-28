using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Payment.Application.Services;

namespace Payment.Application.Abstractions.Services;

public interface IPaymentAuthorizationService
{
    Task<PaymentAuthorizationResult> AuthorizeAsync(OrderCreatedV1 orderCreatedEvent, CancellationToken cancellationToken);
}
