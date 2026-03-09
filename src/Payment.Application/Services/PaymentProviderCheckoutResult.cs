namespace Payment.Application.Services;

public sealed record PaymentProviderCheckoutResult(
    string ProviderCode,
    string ExternalCheckoutId,
    string RedirectUrl,
    string ProviderStatus);

