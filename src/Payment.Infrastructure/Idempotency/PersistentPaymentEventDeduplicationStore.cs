using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions.Idempotency;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Persistence.Entities;

namespace Payment.Infrastructure.Idempotency;

public sealed class PersistentPaymentEventDeduplicationStore(PaymentDbContext dbContext) : IPaymentEventDeduplicationStore
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

        dbContext.ProcessedIntegrationEvents.Add(new ProcessedPaymentIntegrationEvent
        {
            EventId = eventId,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
