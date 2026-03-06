namespace Order.Domain;

public sealed record OrderCompletedDomain(Guid OrderId, string TrackingCode, string TransactionId);
