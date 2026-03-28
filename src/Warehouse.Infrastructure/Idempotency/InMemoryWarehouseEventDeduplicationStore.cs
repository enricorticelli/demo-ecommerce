using System.Collections.Concurrent;
using Warehouse.Application.Abstractions.Idempotency;

namespace Warehouse.Infrastructure.Idempotency;

public sealed class InMemoryWarehouseEventDeduplicationStore : IWarehouseEventDeduplicationStore
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
