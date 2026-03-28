using Cart.Application.Abstractions.Idempotency;
using Cart.Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;
using Shared.BuildingBlocks.Messaging;

namespace Cart.Application.Handlers;

public sealed class SyncCartOnProductUpdatedHandler(
    ICartRepository cartRepository,
    ICartEventDeduplicationStore deduplicationStore,
    ILogger<SyncCartOnProductUpdatedHandler> logger)
    : IntegrationEventHandlerBase<ProductUpdatedV1>(deduplicationStore, logger)
{
    public Task Handle(ProductUpdatedV1 integrationEvent, CancellationToken cancellationToken)
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
                    changed |= cart.UpdateItemSku(integrationEvent.ProductId, integrationEvent.Sku);
                }

                if (changed)
                {
                    await cartRepository.SaveChangesAsync(ct);
                }
            },
            cancellationToken);
    }
}
