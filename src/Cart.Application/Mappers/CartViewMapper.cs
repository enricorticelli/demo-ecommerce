using Cart.Application.Views;
using Shared.BuildingBlocks.Mapping;
using CartEntity = Cart.Domain.Entities.Cart;

namespace Cart.Application.Mappers;

public sealed class CartViewMapper : IViewMapper<CartEntity, CartView>
{
    public CartView Map(CartEntity source)
    {
        return new CartView(
            source.Id,
            source.UserId,
            source.Items.Select(x => new CartItemView(x.ProductId, x.Sku, x.Name, x.Quantity, x.UnitPrice)).ToArray(),
            source.TotalAmount());
    }
}
