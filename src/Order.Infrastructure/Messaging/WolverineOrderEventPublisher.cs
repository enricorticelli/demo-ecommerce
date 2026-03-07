using Order.Application;
using Order.Application.Abstractions;
using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.Contracts.Integration;
using Wolverine;

namespace Order.Infrastructure.Messaging;

public sealed class WolverineOrderEventPublisher(IMessageBus bus) : IOrderEventPublisher
{
    public async Task PublishOrderPlacedAsync(Guid orderId, Guid userId, IReadOnlyList<OrderItemDto> items, decimal totalAmount)
    {
        await bus.PublishAsync(new OrderPlacedV1(orderId, userId, items, totalAmount));
    }

    public async Task PublishOrderFailedAsync(Guid orderId, string reason)
    {
        await bus.PublishAsync(new OrderFailedV1(orderId, reason));
    }

    public async Task PublishOrderCompletedAsync(Guid orderId, Guid cartId, Guid userId, string trackingCode, string transactionId)
    {
        await bus.PublishAsync(new OrderCompletedV1(orderId, cartId, userId, trackingCode, transactionId));
    }

    public async Task RequestPaymentAuthorizationAsync(Guid orderId, Guid userId, decimal amount, string paymentMethod)
    {
        await bus.PublishAsync(new PaymentAuthorizeRequestedV1(orderId, userId, amount, paymentMethod));
    }

    public async Task RequestShippingAsync(Guid orderId, Guid userId, IReadOnlyList<OrderItemDto> items)
    {
        await bus.PublishAsync(new ShippingCreateRequestedV1(orderId, userId, items));
    }
}
