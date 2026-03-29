namespace Order.Api.Contracts.Responses;

public sealed record OrderCreatedResponse(Guid OrderId, string Status);
