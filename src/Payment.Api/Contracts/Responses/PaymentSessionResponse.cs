namespace Payment.Api.Contracts.Responses;

public sealed record PaymentSessionResponse(
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
