namespace Payment.Application.Abstractions.Idempotency;

public interface IPaymentWebhookDeduplicationStore
{
    Task<bool> HasProcessedAsync(string providerCode, string externalEventId, CancellationToken cancellationToken);
    Task MarkProcessedAsync(string providerCode, string externalEventId, CancellationToken cancellationToken);
}

