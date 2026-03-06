namespace Payment.Domain;

public sealed record PaymentSessionRejectedDomain(Guid SessionId, string Reason, DateTimeOffset RejectedAtUtc);
