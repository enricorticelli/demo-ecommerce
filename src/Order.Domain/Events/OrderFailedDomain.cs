namespace Order.Domain;

public sealed record OrderFailedDomain(Guid OrderId, string Reason);
