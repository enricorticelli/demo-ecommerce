using System.Collections.Concurrent;
using Payment.Application.Abstractions.Idempotency;

namespace Payment.Infrastructure.Idempotency;

public sealed class InMemoryPaymentEventDeduplicationStore : IPaymentEventDeduplicationStore
{
    private readonly ConcurrentDictionary<Guid, byte> _processedEvents = new();

    public Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_processedEvents.ContainsKey(eventId));
    }

    public Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        _processedEvents.TryAdd(eventId, 0);
        return Task.CompletedTask;
    }
}
