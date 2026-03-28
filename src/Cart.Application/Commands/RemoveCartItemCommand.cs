namespace Cart.Application.Commands;

public sealed record RemoveCartItemCommand(Guid CartId, Guid ProductId);
