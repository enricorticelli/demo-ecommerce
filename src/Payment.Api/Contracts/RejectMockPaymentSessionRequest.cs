namespace Payment.Api.Contracts;

public sealed record RejectPaymentSessionRequest(string? Reason);
