using Cart.Application;
using Cart.Domain;
using Marten;
using Cart.Infrastructure.Persistence.ReadModels;
using Shared.BuildingBlocks.Contracts;

namespace Cart.Infrastructure;

public sealed class CartService(
    IDocumentSession documentSession,
    IQuerySession querySession,
    ICartReadModelStore cartReadModelStore) : ICartService
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
        await ProjectCartAsync(cartId, cancellationToken);
    }

    public async Task RemoveItemAsync(Guid cartId, Guid productId, CancellationToken cancellationToken)
    {
        var stream = await documentSession.Events.FetchForWriting<CartAggregate>(cartId, cancellationToken);
        stream.AppendOne(new CartItemRemoved(cartId, productId));
        await documentSession.SaveChangesAsync(cancellationToken);
        await ProjectCartAsync(cartId, cancellationToken);
    }

    public async Task<CartView?> GetCartAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var readModel = await cartReadModelStore.GetAsync(cartId, cancellationToken);
        if (readModel is null)
        {
            return null;
        }

        return new CartView(readModel.CartId, readModel.UserId, readModel.Items, readModel.TotalAmount);
    }

    public async Task<CartCheckedOutV1?> CheckoutAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var readModel = await cartReadModelStore.GetAsync(cartId, cancellationToken);
        if (readModel is null || readModel.Items.Count == 0)
        {
            return null;
        }

        return new CartCheckedOutV1(
            cartId,
            Guid.NewGuid(),
            readModel.UserId,
            readModel.Items,
            readModel.TotalAmount);
    }

    private async Task ProjectCartAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await querySession.Events.AggregateStreamAsync<CartAggregate>(cartId, token: cancellationToken);
        if (cart is null)
        {
            return;
        }

        var items = cart.Lines.Values
            .Select(line => new OrderItemDto(line.ProductId, line.Sku, line.Name, line.Quantity, line.UnitPrice))
            .ToArray();

        await cartReadModelStore.UpsertAsync(
            new CartReadModelRow(cart.Id, cart.UserId, items, cart.TotalAmount),
            cancellationToken);
    }
}
