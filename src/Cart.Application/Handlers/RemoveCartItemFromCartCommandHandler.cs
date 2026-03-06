using Shared.BuildingBlocks.Cqrs;

namespace Cart.Application;

public sealed class RemoveCartItemFromCartCommandHandler(ICartService cartService)
    : ICommandHandler<RemoveCartItemFromCartCommand, Unit>
{
    public async Task<Unit> HandleAsync(RemoveCartItemFromCartCommand command, CancellationToken cancellationToken)
    {
        await cartService.RemoveItemAsync(command.CartId, command.ProductId, cancellationToken);
        return Unit.Value;
    }
}
