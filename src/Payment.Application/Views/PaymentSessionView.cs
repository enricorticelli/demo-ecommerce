namespace Payment.Application.Views;

public sealed record PaymentSessionView(
    Guid SessionId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string PaymentMethod,
    string Status,
    string? TransactionId,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string RedirectUrl);
