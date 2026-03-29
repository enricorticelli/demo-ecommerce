namespace Cart.Api.Contracts.Responses;

public sealed record AddCartItemResponse(Guid CartId, string Message);
