using Cart.Application.Abstractions.Idempotency;
using Cart.Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;
using Shared.BuildingBlocks.Messaging;

namespace Cart.Application.Handlers;

public sealed class SyncCartOnProductDeletedHandler(
    ICartRepository cartRepository,
    ICartEventDeduplicationStore deduplicationStore,
    ILogger<SyncCartOnProductDeletedHandler> logger)
    : IntegrationEventHandlerBase<ProductDeletedV1>(deduplicationStore, logger)
{
    public Task Handle(ProductDeletedV1 integrationEvent, CancellationToken cancellationToken)
    {
        return HandleDeduplicatedAsync(
            integrationEvent,
            async ct =>
            {
                var carts = await cartRepository.ListByProductIdAsync(integrationEvent.ProductId, ct);
                if (carts.Count == 0)
                {
                    return;
                }

                var changed = false;
                foreach (var cart in carts)
                {
                    changed |= cart.RemoveItem(integrationEvent.ProductId);
                }

                if (changed)
                {
                    await cartRepository.SaveChangesAsync(ct);
                }
            },
            cancellationToken);
    }
}
