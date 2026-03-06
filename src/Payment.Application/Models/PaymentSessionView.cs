namespace Payment.Application;

public sealed record PaymentSessionView(
    Guid SessionId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Status,
    string? TransactionId,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string RedirectUrl);
