using Marten;
using Order.Api.Domain;
using Shared.BuildingBlocks.Contracts;
using Wolverine;

namespace Order.Api.Handlers;

public class OrderWorkflowHandlers
{
    public static async Task Handle(StockReservedV1 message, IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var stream = await session.Events.FetchForWriting<OrderAggregate>(message.OrderId, cancellationToken);
        var current = await session.Events.AggregateStreamAsync<OrderAggregate>(message.OrderId, token: cancellationToken);
        if (current is null || current.Status is OrderStatus.Completed or OrderStatus.Failed)
        {
            return;
        }

        stream.AppendOne(new OrderStockReservedDomain(message.OrderId));
        await session.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new PaymentAuthorizeRequestedV1(message.OrderId, current.UserId, current.TotalAmount));
    }

    public static async Task Handle(StockRejectedV1 message, IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var stream = await session.Events.FetchForWriting<OrderAggregate>(message.OrderId, cancellationToken);
        stream.AppendOne(new OrderFailedDomain(message.OrderId, message.Reason));
        await session.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new OrderFailedV1(message.OrderId, message.Reason));
    }

    public static async Task Handle(PaymentAuthorizedV1 message, IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var stream = await session.Events.FetchForWriting<OrderAggregate>(message.OrderId, cancellationToken);
        var current = await session.Events.AggregateStreamAsync<OrderAggregate>(message.OrderId, token: cancellationToken);
        if (current is null || current.Status is OrderStatus.Completed or OrderStatus.Failed)
        {
            return;
        }

        stream.AppendOne(new OrderPaymentAuthorizedDomain(message.OrderId, message.TransactionId));
        await session.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new ShippingCreateRequestedV1(message.OrderId, current.UserId, current.Items));
    }

    public static async Task Handle(PaymentFailedV1 message, IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var stream = await session.Events.FetchForWriting<OrderAggregate>(message.OrderId, cancellationToken);
        stream.AppendOne(new OrderFailedDomain(message.OrderId, message.Reason));
        await session.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new OrderFailedV1(message.OrderId, message.Reason));
    }

    public static async Task Handle(ShippingCreatedV1 message, IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var stream = await session.Events.FetchForWriting<OrderAggregate>(message.OrderId, cancellationToken);
        var current = await session.Events.AggregateStreamAsync<OrderAggregate>(message.OrderId, token: cancellationToken);
        if (current is null || current.Status is OrderStatus.Completed or OrderStatus.Failed)
        {
            return;
        }

        stream.AppendOne(new OrderCompletedDomain(message.OrderId, message.TrackingCode, current.TransactionId));
        await session.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new OrderCompletedV1(message.OrderId, message.TrackingCode, current.TransactionId));
    }
}
