namespace Payment.Application;

public sealed record PaymentAuthorizationResult(
    Guid OrderId,
    bool Authorized,
    string? TransactionId = null,
    Guid? PaymentSessionId = null);
