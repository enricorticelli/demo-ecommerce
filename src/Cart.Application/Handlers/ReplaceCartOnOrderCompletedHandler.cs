using Cart.Application.Abstractions.Idempotency;
using Cart.Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Messaging;

namespace Cart.Application.Handlers;

public sealed class ReplaceCartOnOrderCompletedHandler(
    ICartRepository cartRepository,
    ICartEventDeduplicationStore deduplicationStore,
    ILogger<ReplaceCartOnOrderCompletedHandler> logger)
    : IntegrationEventHandlerBase<OrderCompletedV1>(deduplicationStore, logger)
{
    public Task Handle(OrderCompletedV1 integrationEvent, CancellationToken cancellationToken)
    {
        return HandleDeduplicatedAsync(
            integrationEvent,
            async ct =>
            {
                var cart = await cartRepository.GetByIdAsync(integrationEvent.CartId, ct);
                if (cart is null)
                {
                    return;
                }

                cartRepository.Remove(cart);
                cartRepository.Add(Domain.Entities.Cart.Create(Guid.NewGuid(), integrationEvent.UserId));
                await cartRepository.SaveChangesAsync(ct);
            },
            cancellationToken);
    }
}
