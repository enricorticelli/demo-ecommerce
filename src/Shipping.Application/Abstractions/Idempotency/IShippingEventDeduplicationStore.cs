namespace Shipping.Application.Abstractions.Idempotency;

public interface IShippingEventDeduplicationStore
{
    Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken);
}
