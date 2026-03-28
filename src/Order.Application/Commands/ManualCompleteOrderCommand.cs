namespace Order.Application.Commands;

public sealed record ManualCompleteOrderCommand(Guid OrderId, string? TrackingCode, string? TransactionId);
