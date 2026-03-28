using CartEntity = Cart.Domain.Entities.Cart;

namespace Cart.Application.Abstractions.Repositories;

public interface ICartRepository
{
    Task<CartEntity?> GetByIdAsync(Guid cartId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CartEntity>> ListByProductIdAsync(Guid productId, CancellationToken cancellationToken);
    void Add(CartEntity cart);
    void Remove(CartEntity cart);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
