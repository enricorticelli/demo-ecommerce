using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.Cqrs;

namespace Cart.Application;

public sealed record CheckoutCartCommand(Guid CartId) : ICommand<CartCheckedOutV1?>;
