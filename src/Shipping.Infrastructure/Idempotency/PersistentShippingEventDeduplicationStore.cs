using Microsoft.EntityFrameworkCore;
using Shipping.Application.Abstractions.Idempotency;
using Shipping.Infrastructure.Persistence;
using Shipping.Infrastructure.Persistence.Entities;

namespace Shipping.Infrastructure.Idempotency;

public sealed class PersistentShippingEventDeduplicationStore(ShippingDbContext dbContext) : IShippingEventDeduplicationStore
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

        dbContext.ProcessedIntegrationEvents.Add(new ProcessedShippingIntegrationEvent
        {
            EventId = eventId,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
