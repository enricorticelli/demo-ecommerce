namespace Payment.Application.Views;

public sealed record PaymentSessionView(
    Guid SessionId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string PaymentMethod,
    string ProviderCode,
    string? ExternalCheckoutId,
    string ProviderStatus,
    string Status,
    string? TransactionId,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string RedirectUrl);
