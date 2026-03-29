namespace Order.Api.Contracts.Responses;

public sealed record ManualCancelOrderResponse(
    Guid OrderId,
    string Status,
    string Reason,
    string Mode);
