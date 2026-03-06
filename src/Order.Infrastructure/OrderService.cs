using Order.Application;
using Order.Domain;

namespace Order.Infrastructure;

public sealed class OrderService(
    ICartSnapshotClient cartSnapshotClient,
    IWarehouseClient warehouseClient,
    IOrderStateStore orderStateStore,
    IOrderEventPublisher eventPublisher) : IOrderService
{
    public async Task<OrderCreationResult?> CreateOrderAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var cart = await cartSnapshotClient.GetCartAsync(command.CartId, cancellationToken);
        if (cart is null)
        {
            return null;
        }

        if (cart.Items.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty or unreadable");
        }

        var orderId = Guid.NewGuid();
        await orderStateStore.StartOrderAsync(orderId, command.CartId, command.UserId, cart.Items, cart.TotalAmount, cancellationToken);
        await eventPublisher.PublishOrderPlacedAsync(orderId, command.UserId, cart.Items, cart.TotalAmount);

        var stockReservation = await warehouseClient.ReserveStockAsync(orderId, cart.Items, cancellationToken);
        if (!stockReservation.Reserved)
        {
            var reason = stockReservation.Reason ?? "Stock not available";
            await orderStateStore.MarkFailedAsync(orderId, reason, cancellationToken);
            await eventPublisher.PublishOrderFailedAsync(orderId, reason);
            return new OrderCreationResult(orderId, OrderStatus.Failed.ToString());
        }

        await orderStateStore.MarkStockReservedAsync(orderId, cancellationToken);
        await eventPublisher.RequestPaymentAuthorizationAsync(orderId, command.UserId, cart.TotalAmount);
        return new OrderCreationResult(orderId, OrderStatus.StockReserved.ToString());
    }

    public Task<OrderView?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return orderStateStore.GetOrderViewAsync(orderId, cancellationToken);
    }
}
