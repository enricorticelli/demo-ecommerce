namespace Cart.Api.Contracts.Responses;

public sealed record RemoveCartItemResponse(Guid CartId, Guid ProductId, string Message);
