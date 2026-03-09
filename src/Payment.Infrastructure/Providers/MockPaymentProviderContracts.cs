namespace Payment.Infrastructure.Providers;

internal sealed record MockProviderCheckoutCreateRequest(
    Guid SessionId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string PaymentMethod,
    string ReturnUrl,
    string CancelUrl);

internal sealed record MockProviderCheckoutCreateResponse(
    string ProviderCode,
    string ExternalCheckoutId,
    string RedirectUrl,
    string Status);

internal sealed record MockProviderWebhookPayload(
    string EventId,
    Guid? SessionId,
    string? ExternalCheckoutId,
    string Status,
    string? TransactionId,
    string? Reason);

