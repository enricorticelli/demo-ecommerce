using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions.Idempotency;
using Warehouse.Infrastructure.Persistence;
using Warehouse.Infrastructure.Persistence.Entities;

namespace Warehouse.Infrastructure.Idempotency;

public sealed class PersistentWarehouseEventDeduplicationStore(WarehouseDbContext dbContext) : IWarehouseEventDeduplicationStore
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

        dbContext.ProcessedIntegrationEvents.Add(new ProcessedWarehouseIntegrationEvent
        {
            EventId = eventId,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
