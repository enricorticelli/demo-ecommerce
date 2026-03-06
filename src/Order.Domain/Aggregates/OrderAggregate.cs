using Shared.BuildingBlocks.Contracts;

namespace Order.Domain;

public sealed class OrderAggregate
{
    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public string FailureReason { get; private set; } = string.Empty;
    public string TrackingCode { get; private set; } = string.Empty;
    public string TransactionId { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public List<OrderItemDto> Items { get; } = [];

    public void Apply(OrderPlacedDomain @event)
    {
        Id = @event.OrderId;
        CartId = @event.CartId;
        UserId = @event.UserId;
        TotalAmount = @event.TotalAmount;
        Items.Clear();
        Items.AddRange(@event.Items);
        Status = OrderStatus.Pending;
    }

    public void Apply(OrderStockReservedDomain _)
    {
        if (Status != OrderStatus.Failed)
        {
            Status = OrderStatus.StockReserved;
        }
    }

    public void Apply(OrderPaymentAuthorizedDomain @event)
    {
        if (Status != OrderStatus.Failed)
        {
            Status = OrderStatus.PaymentAuthorized;
            TransactionId = @event.TransactionId;
        }
    }

    public void Apply(OrderCompletedDomain @event)
    {
        if (Status != OrderStatus.Failed)
        {
            Status = OrderStatus.Completed;
            TrackingCode = @event.TrackingCode;
            TransactionId = @event.TransactionId;
        }
    }

    public void Apply(OrderFailedDomain @event)
    {
        Status = OrderStatus.Failed;
        FailureReason = @event.Reason;
    }
}
