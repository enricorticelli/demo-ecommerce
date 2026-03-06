using Shared.BuildingBlocks.Cqrs;

namespace Cart.Application;

public sealed class AddCartItemToCartCommandHandler(ICartService cartService)
    : ICommandHandler<AddCartItemToCartCommand, Unit>
{
    public async Task<Unit> HandleAsync(AddCartItemToCartCommand command, CancellationToken cancellationToken)
    {
        await cartService.AddItemAsync(command.CartId, command.Item, cancellationToken);
        return Unit.Value;
    }
}
