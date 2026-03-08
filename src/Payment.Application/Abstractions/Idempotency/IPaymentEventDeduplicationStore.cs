namespace Payment.Application.Abstractions.Idempotency;

public interface IPaymentEventDeduplicationStore
{
    Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken);
}
