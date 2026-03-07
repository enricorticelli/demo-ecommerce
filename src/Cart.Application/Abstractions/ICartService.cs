using Cart.Application.Views;
using Shared.BuildingBlocks.Contracts.Integration;

namespace Cart.Application.Abstractions;

public interface ICartService
{
    Task AddItemAsync(Guid cartId, AddCartItemCommand command, CancellationToken cancellationToken);
    Task RemoveItemAsync(Guid cartId, Guid productId, CancellationToken cancellationToken);
    Task<CartView?> GetCartAsync(Guid cartId, CancellationToken cancellationToken);
    Task<CartCheckedOutV1?> CheckoutAsync(Guid cartId, CancellationToken cancellationToken);
    Task RotateCartAfterOrderCompletionAsync(Guid cartId, Guid userId, Guid orderId, CancellationToken cancellationToken);
}
