using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.Cqrs;

namespace Cart.Application;

public sealed class CheckoutCartCommandHandler(ICartService cartService)
    : ICommandHandler<CheckoutCartCommand, CartCheckedOutV1?>
{
    public Task<CartCheckedOutV1?> HandleAsync(CheckoutCartCommand command, CancellationToken cancellationToken)
    {
        return cartService.CheckoutAsync(command.CartId, cancellationToken);
    }
}
