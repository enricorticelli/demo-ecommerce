using Shared.BuildingBlocks.Contracts;

namespace Order.Application;

public interface IOrderStateStore
{
    Task StartOrderAsync(Guid orderId, Guid cartId, Guid userId, IReadOnlyList<OrderItemDto> items, decimal totalAmount, CancellationToken cancellationToken);
    Task MarkStockReservedAsync(Guid orderId, CancellationToken cancellationToken);
    Task MarkPaymentAuthorizedAsync(Guid orderId, string transactionId, CancellationToken cancellationToken);
    Task MarkCompletedAsync(Guid orderId, string trackingCode, string transactionId, CancellationToken cancellationToken);
    Task MarkFailedAsync(Guid orderId, string reason, CancellationToken cancellationToken);
}
