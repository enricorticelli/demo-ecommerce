using Shared.BuildingBlocks.Cqrs;

namespace Cart.Application;

public sealed record RemoveCartItemFromCartCommand(Guid CartId, Guid ProductId) : ICommand<Unit>;
