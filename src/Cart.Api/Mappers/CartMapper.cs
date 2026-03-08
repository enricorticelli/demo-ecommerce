using Cart.Api.Contracts.Requests;
using Cart.Api.Contracts.Responses;
using Cart.Application.Commands;
using Cart.Application.Views;

namespace Cart.Api.Mappers;

public static class CartMapper
{
	public static AddCartItemCommand ToCommand(this AddCartItemRequest request, Guid cartId)
	{
		return new AddCartItemCommand(
			cartId,
			request.UserId,
			request.ProductId,
			request.Sku,
			request.Name,
			request.Quantity,
			request.UnitPrice);
	}

	public static CartResponse ToResponse(this CartView view)
	{
		return new CartResponse(
			view.CartId,
			view.UserId,
			view.Items.Select(x => new CartItemResponse(x.ProductId, x.Sku, x.Name, x.Quantity, x.UnitPrice)).ToArray(),
			view.TotalAmount);
	}

	public static CheckoutCartResponse ToResponse(this CheckoutCartView view)
	{
		return new CheckoutCartResponse(
			view.CartId,
			view.OrderId,
			view.UserId,
			view.Items.Select(x => new CartItemResponse(x.ProductId, x.Sku, x.Name, x.Quantity, x.UnitPrice)).ToArray(),
			view.TotalAmount);
	}
}
