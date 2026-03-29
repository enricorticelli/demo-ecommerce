namespace Cart.Api.Contracts.Responses;

public sealed record CheckoutCartResponse(Guid CartId, Guid OrderId, Guid UserId, IReadOnlyList<CartItemResponse> Items, decimal TotalAmount);
