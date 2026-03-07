using Order.Application;
using Order.Application.Abstractions;
using Order.Domain;
using Order.Domain.Enums;
using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.Contracts.Integration;

namespace Order.Infrastructure;

public sealed class OrderWorkflowProcessor(IOrderStateReader orderStateReader, IOrderStateStore orderStateStore, IOrderEventPublisher eventPublisher) : IOrderWorkflowProcessor
{
    public async Task HandleStockReservedAsync(StockReservedV1 message, CancellationToken cancellationToken)
    {
        var state = await orderStateReader.GetAsync(message.OrderId, cancellationToken);
        if (state is null || IsTerminal(state.Status))
        {
            return;
        }

        await orderStateStore.MarkStockReservedAsync(message.OrderId, cancellationToken);
        await eventPublisher.RequestPaymentAuthorizationAsync(message.OrderId, state.UserId, state.TotalAmount, state.PaymentMethod);
    }

    public async Task HandleStockRejectedAsync(StockRejectedV1 message, CancellationToken cancellationToken)
    {
        await orderStateStore.MarkFailedAsync(message.OrderId, message.Reason, cancellationToken);
        await eventPublisher.PublishOrderFailedAsync(message.OrderId, message.Reason);
    }

    public async Task HandlePaymentAuthorizedAsync(PaymentAuthorizedV1 message, CancellationToken cancellationToken)
    {
        var state = await orderStateReader.GetAsync(message.OrderId, cancellationToken);
        if (state is null || IsTerminal(state.Status))
        {
            return;
        }

        await orderStateStore.MarkPaymentAuthorizedAsync(message.OrderId, message.TransactionId, cancellationToken);
        await eventPublisher.RequestShippingAsync(message.OrderId, state.UserId, state.Items);
    }

    public async Task HandlePaymentFailedAsync(PaymentFailedV1 message, CancellationToken cancellationToken)
    {
        await orderStateStore.MarkFailedAsync(message.OrderId, message.Reason, cancellationToken);
        await eventPublisher.PublishOrderFailedAsync(message.OrderId, message.Reason);
    }

    public async Task HandleShippingCreatedAsync(ShippingCreatedV1 message, CancellationToken cancellationToken)
    {
        var state = await orderStateReader.GetAsync(message.OrderId, cancellationToken);
        if (state is null || IsTerminal(state.Status))
        {
            return;
        }

        await orderStateStore.MarkCompletedAsync(message.OrderId, message.TrackingCode, state.TransactionId, cancellationToken);
        await eventPublisher.PublishOrderCompletedAsync(
            message.OrderId,
            state.CartId,
            state.UserId,
            message.TrackingCode,
            state.TransactionId);
    }

    private static bool IsTerminal(OrderStatus status)
    {
        return status is OrderStatus.Completed or OrderStatus.Failed;
    }
}
