namespace Order.Domain;

public sealed record OrderPaymentAuthorizedDomain(Guid OrderId, string TransactionId);
