using Cart.Application;
using Cart.Domain;
using Marten;
using Shared.BuildingBlocks.Contracts;

namespace Cart.Infrastructure;

public sealed class CartService(IDocumentSession documentSession, IQuerySession querySession) : ICartService
{
    public async Task AddItemAsync(Guid cartId, AddCartItemCommand command, CancellationToken cancellationToken)
    {
        var stream = await documentSession.Events.FetchForWriting<CartAggregate>(cartId, cancellationToken);
        if (!stream.Events.Any())
        {
            stream.AppendOne(new CartCreated(cartId, command.UserId));
        }

        stream.AppendOne(new CartItemAdded(cartId, command.ProductId, command.Sku, command.Name, command.Quantity, command.UnitPrice));
        await documentSession.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveItemAsync(Guid cartId, Guid productId, CancellationToken cancellationToken)
    {
        var stream = await documentSession.Events.FetchForWriting<CartAggregate>(cartId, cancellationToken);
        stream.AppendOne(new CartItemRemoved(cartId, productId));
        await documentSession.SaveChangesAsync(cancellationToken);
    }

    public async Task<CartView?> GetCartAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await querySession.Events.AggregateStreamAsync<CartAggregate>(cartId, token: cancellationToken);
        if (cart is null)
        {
            return null;
        }

        var items = cart.Lines.Values
            .Select(line => new OrderItemDto(line.ProductId, line.Sku, line.Name, line.Quantity, line.UnitPrice))
            .ToList();

        return new CartView(cart.Id, cart.UserId, items, cart.TotalAmount);
    }

    public async Task<CartCheckedOutV1?> CheckoutAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await querySession.Events.AggregateStreamAsync<CartAggregate>(cartId, token: cancellationToken);
        if (cart is null || cart.Lines.Count == 0)
        {
            return null;
        }

        return new CartCheckedOutV1(
            cartId,
            Guid.NewGuid(),
            cart.UserId,
            cart.Lines.Values.Select(line => new OrderItemDto(line.ProductId, line.Sku, line.Name, line.Quantity, line.UnitPrice)).ToList(),
            cart.TotalAmount);
    }
}
