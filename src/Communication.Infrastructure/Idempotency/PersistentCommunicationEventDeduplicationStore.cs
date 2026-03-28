using Communication.Application.Abstractions.Idempotency;
using Communication.Infrastructure.Persistence;
using Communication.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Communication.Infrastructure.Idempotency;

public sealed class PersistentCommunicationEventDeduplicationStore(CommunicationDbContext dbContext)
    : ICommunicationEventDeduplicationStore
{
    public Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return dbContext.ProcessedIntegrationEvents.AnyAsync(x => x.EventId == eventId, cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        if (await HasProcessedAsync(eventId, cancellationToken))
        {
            return;
        }

        dbContext.ProcessedIntegrationEvents.Add(new ProcessedCommunicationIntegrationEvent
        {
            EventId = eventId,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
