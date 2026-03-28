using Cart.Application.Commands;
using Cart.Application.Views;

namespace Cart.Application.Abstractions.Commands;

public interface ICartCommandService
{
    Task<CartView> AddItemAsync(AddCartItemCommand command, CancellationToken cancellationToken);
    Task<CartView> RemoveItemAsync(RemoveCartItemCommand command, CancellationToken cancellationToken);
    Task<CheckoutCartView> CheckoutAsync(CheckoutCartCommand command, CancellationToken cancellationToken);
}
