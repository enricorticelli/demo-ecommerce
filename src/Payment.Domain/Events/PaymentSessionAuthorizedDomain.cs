namespace Payment.Domain;

public sealed record PaymentSessionAuthorizedDomain(Guid SessionId, string TransactionId, DateTimeOffset AuthorizedAtUtc);
