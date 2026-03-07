using Shared.BuildingBlocks.Contracts.Integration;

namespace Order.Application.Abstractions;

public interface IOrderEventPublisher
{
    Task PublishOrderPlacedAsync(Guid orderId, Guid userId, IReadOnlyList<OrderItemDto> items, decimal totalAmount);
    Task PublishOrderFailedAsync(Guid orderId, string reason);
    Task PublishOrderCompletedAsync(Guid orderId, Guid cartId, Guid userId, string trackingCode, string transactionId);
    Task RequestPaymentAuthorizationAsync(Guid orderId, Guid userId, decimal amount, string paymentMethod);
    Task RequestShippingAsync(Guid orderId, Guid userId, IReadOnlyList<OrderItemDto> items);
}
