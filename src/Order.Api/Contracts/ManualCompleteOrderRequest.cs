namespace Order.Api.Contracts;

public sealed record ManualCompleteOrderRequest(string? TrackingCode, string? TransactionId);
