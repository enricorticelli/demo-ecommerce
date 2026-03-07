using Catalog.Infrastructure.Persistence;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.Messaging;
using Wolverine.EntityFrameworkCore;

namespace Catalog.Infrastructure.Messaging;

public sealed class OutboxDomainEventPublisher(IDbContextOutbox<CatalogDbContext> outbox) : IDomainEventPublisher
{
    public async Task PublishAndFlushAsync(IntegrationEventBase integrationEvent, CancellationToken cancellationToken)
    {
        await outbox.PublishAsync(integrationEvent);
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
