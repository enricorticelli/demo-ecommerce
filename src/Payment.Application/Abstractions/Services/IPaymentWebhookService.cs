using Payment.Application.Services;

namespace Payment.Application.Abstractions.Services;

public interface IPaymentWebhookService
{
    Task<PaymentWebhookProcessResult> ProcessAsync(
        string providerCode,
        string rawPayload,
        IReadOnlyDictionary<string, string> headers,
        string correlationId,
        CancellationToken cancellationToken);
}

