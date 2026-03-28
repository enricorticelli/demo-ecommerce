namespace Order.Application.Commands;

public sealed record ManualCancelOrderCommand(Guid OrderId, string? Reason);
