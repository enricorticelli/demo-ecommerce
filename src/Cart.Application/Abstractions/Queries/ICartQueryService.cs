using Cart.Application.Views;

namespace Cart.Application.Abstractions.Queries;

public interface ICartQueryService
{
    Task<CartView> GetByIdAsync(Guid cartId, CancellationToken cancellationToken);
}
