using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions.Idempotency;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Persistence.Entities;

namespace Order.Infrastructure.Idempotency;

public sealed class PersistentOrderEventDeduplicationStore(OrderDbContext dbContext) : IOrderEventDeduplicationStore
{
    public Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return dbContext.ProcessedIntegrationEvents
            .AnyAsync(x => x.EventId == eventId, cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        if (await HasProcessedAsync(eventId, cancellationToken))
        {
            return;
        }

        dbContext.ProcessedIntegrationEvents.Add(new ProcessedOrderIntegrationEvent
        {
            EventId = eventId,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
