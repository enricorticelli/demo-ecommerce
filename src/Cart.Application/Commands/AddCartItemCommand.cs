namespace Cart.Application.Commands;

public sealed record AddCartItemCommand(
    Guid CartId,
    Guid UserId,
    Guid ProductId,
    string Sku,
    string Name,
    int Quantity,
    decimal UnitPrice);
