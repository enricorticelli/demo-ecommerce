using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions.Idempotency;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Persistence.Entities;

namespace Payment.Infrastructure.Idempotency;

public sealed class PersistentPaymentWebhookDeduplicationStore(PaymentDbContext dbContext) : IPaymentWebhookDeduplicationStore
{
    public Task<bool> HasProcessedAsync(string providerCode, string externalEventId, CancellationToken cancellationToken)
    {
        var normalizedProviderCode = providerCode.Trim().ToLowerInvariant();
        var normalizedExternalEventId = externalEventId.Trim();

        return dbContext.ProcessedWebhookEvents.AnyAsync(
            x => x.ProviderCode == normalizedProviderCode && x.ExternalEventId == normalizedExternalEventId,
            cancellationToken);
    }

    public async Task MarkProcessedAsync(string providerCode, string externalEventId, CancellationToken cancellationToken)
    {
        var normalizedProviderCode = providerCode.Trim().ToLowerInvariant();
        var normalizedExternalEventId = externalEventId.Trim();

        if (await HasProcessedAsync(normalizedProviderCode, normalizedExternalEventId, cancellationToken))
        {
            return;
        }

        dbContext.ProcessedWebhookEvents.Add(new ProcessedPaymentWebhookEvent
        {
            ProviderCode = normalizedProviderCode,
            ExternalEventId = normalizedExternalEventId,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

