using Cart.Application.Abstractions.Queries;
using Cart.Application.Abstractions.Repositories;
using Cart.Application.Views;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;

namespace Cart.Application.Services;

public sealed class CartQueryService(
    ICartRepository cartRepository,
    IViewMapper<Cart.Domain.Entities.Cart, CartView> mapper) : ICartQueryService
{
    public async Task<CartView> GetByIdAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByIdAsync(cartId, cancellationToken)
            ?? throw new NotFoundAppException($"Cart '{cartId}' not found.");

        return mapper.Map(cart);
    }
}
