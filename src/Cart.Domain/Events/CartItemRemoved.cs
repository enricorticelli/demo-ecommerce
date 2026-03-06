namespace Cart.Domain;

public sealed record CartItemRemoved(Guid CartId, Guid ProductId);
