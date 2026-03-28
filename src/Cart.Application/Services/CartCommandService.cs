using Cart.Application.Abstractions.Commands;
using Cart.Application.Abstractions.Repositories;
using Cart.Application.Commands;
using Cart.Application.Views;
using Cart.Domain.Entities;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;

namespace Cart.Application.Services;

public sealed class CartCommandService(
    ICartRepository cartRepository,
    IViewMapper<Cart.Domain.Entities.Cart, CartView> mapper) : ICartCommandService
{
    public async Task<CartView> AddItemAsync(AddCartItemCommand command, CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByIdAsync(command.CartId, cancellationToken);
        var isNewCart = cart is null;
        cart ??= Cart.Domain.Entities.Cart.Create(command.CartId, command.UserId);

        cart.AddItem(CartItem.Create(command.ProductId, command.Sku, command.Name, command.Quantity, command.UnitPrice));

        if (isNewCart)
        {
            cartRepository.Add(cart);
        }

        await cartRepository.SaveChangesAsync(cancellationToken);
        return mapper.Map(cart);
    }

    public async Task<CartView> RemoveItemAsync(RemoveCartItemCommand command, CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByIdAsync(command.CartId, cancellationToken)
            ?? throw new NotFoundAppException($"Cart '{command.CartId}' not found.");

        _ = cart.RemoveItem(command.ProductId);
        await cartRepository.SaveChangesAsync(cancellationToken);

        return mapper.Map(cart);
    }

    public async Task<CheckoutCartView> CheckoutAsync(CheckoutCartCommand command, CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByIdAsync(command.CartId, cancellationToken)
            ?? throw new NotFoundAppException($"Cart '{command.CartId}' not found.");

        if (cart.Items.Count == 0)
        {
            throw new ValidationAppException("Cart is empty.");
        }

        var result = new CheckoutCartView(
            cart.Id,
            Guid.NewGuid(),
            cart.UserId,
            cart.Items.Select(x => new CartItemView(x.ProductId, x.Sku, x.Name, x.Quantity, x.UnitPrice)).ToArray(),
            cart.TotalAmount());

        return result;
    }
}
