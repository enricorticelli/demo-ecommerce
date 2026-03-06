using Shared.BuildingBlocks.Cqrs;

namespace Cart.Application;

public sealed record AddCartItemToCartCommand(Guid CartId, AddCartItemCommand Item) : ICommand<Unit>;
