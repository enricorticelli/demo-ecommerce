namespace Cart.Application.Views;

public sealed record CheckoutCartView(Guid CartId, Guid OrderId, Guid UserId, IReadOnlyList<CartItemView> Items, decimal TotalAmount);
