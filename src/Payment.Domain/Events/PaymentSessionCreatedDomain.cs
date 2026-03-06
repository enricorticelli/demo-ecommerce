namespace Payment.Domain;

public sealed record PaymentSessionCreatedDomain(Guid SessionId, Guid OrderId, Guid UserId, decimal Amount, DateTimeOffset CreatedAtUtc);
