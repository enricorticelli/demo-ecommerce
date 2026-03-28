namespace Payment.Application.Services;

public sealed record PaymentAuthorizationResult(Guid OrderId, bool IsAuthorized, string? TransactionId, string? FailureReason);
