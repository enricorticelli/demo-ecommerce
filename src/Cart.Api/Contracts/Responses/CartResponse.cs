namespace Cart.Api.Contracts.Responses;

public sealed record CartResponse(Guid CartId, Guid UserId, IReadOnlyList<CartItemResponse> Items, decimal TotalAmount);
