using Shared.BuildingBlocks.Cqrs;

namespace Cart.Application;

public sealed class GetCartByIdQueryHandler(ICartService cartService)
    : IQueryHandler<GetCartByIdQuery, CartView?>
{
    public Task<CartView?> HandleAsync(GetCartByIdQuery query, CancellationToken cancellationToken)
    {
        return cartService.GetCartAsync(query.CartId, cancellationToken);
    }
}
