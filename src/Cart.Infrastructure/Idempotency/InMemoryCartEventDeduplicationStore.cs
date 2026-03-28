using Cart.Application.Abstractions.Idempotency;
using System.Collections.Concurrent;

namespace Cart.Infrastructure.Idempotency;

public sealed class InMemoryCartEventDeduplicationStore : ICartEventDeduplicationStore
{
    private static readonly ConcurrentDictionary<Guid, byte> ProcessedEvents = new();

    public Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return Task.FromResult(ProcessedEvents.ContainsKey(eventId));
    }

    public Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        ProcessedEvents.TryAdd(eventId, 0);
        return Task.CompletedTask;
    }
}
