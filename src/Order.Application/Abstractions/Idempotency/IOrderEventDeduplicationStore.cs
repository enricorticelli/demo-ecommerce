namespace Order.Application.Abstractions.Idempotency;

public interface IOrderEventDeduplicationStore
{
    Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken);
}
