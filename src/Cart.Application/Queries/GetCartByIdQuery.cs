using Shared.BuildingBlocks.Cqrs;

namespace Cart.Application;

public sealed record GetCartByIdQuery(Guid CartId) : IQuery<CartView?>;
