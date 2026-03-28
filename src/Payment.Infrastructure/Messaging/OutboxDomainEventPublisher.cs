using Payment.Infrastructure.Persistence;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.Messaging;
using Wolverine.EntityFrameworkCore;

namespace Payment.Infrastructure.Messaging;

public sealed class OutboxDomainEventPublisher(IDbContextOutbox<PaymentDbContext> outbox) : IDomainEventPublisher
{
    public async Task PublishAndFlushAsync(IntegrationEventBase integrationEvent, CancellationToken cancellationToken)
    {
        await outbox.PublishAsync(integrationEvent);
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }

    public async Task PublishBatchAndFlushAsync(IReadOnlyCollection<IntegrationEventBase> integrationEvents, CancellationToken cancellationToken)
    {
        foreach (var integrationEvent in integrationEvents)
        {
            await outbox.PublishAsync(integrationEvent);
        }

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
