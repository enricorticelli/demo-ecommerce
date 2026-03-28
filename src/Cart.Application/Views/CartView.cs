namespace Cart.Application.Views;

public sealed record CartView(Guid CartId, Guid UserId, IReadOnlyList<CartItemView> Items, decimal TotalAmount);
