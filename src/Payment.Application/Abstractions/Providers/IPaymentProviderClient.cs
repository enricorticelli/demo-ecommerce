using Payment.Application.Services;

namespace Payment.Application.Abstractions.Providers;

public interface IPaymentProviderClient
{
    string ProviderCode { get; }
    bool SupportsPaymentMethod(string paymentMethod);
    Task<PaymentProviderCheckoutResult> CreateCheckoutAsync(PaymentProviderCheckoutRequest request, CancellationToken cancellationToken);
    PaymentWebhookNotification ParseWebhook(string rawPayload);
}

