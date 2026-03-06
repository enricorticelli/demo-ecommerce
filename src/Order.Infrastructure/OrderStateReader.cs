using Order.Application;
using Order.Domain;
using Order.Infrastructure.Persistence.ReadModels;

namespace Order.Infrastructure;

public sealed class OrderStateReader(IOrderReadModelStore orderReadModelStore) : IOrderStateReader
{
    public async Task<OrderAggregateState?> GetAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var readModel = await orderReadModelStore.GetAsync(orderId, cancellationToken);
        if (readModel is null)
        {
            return null;
        }

        var status = Enum.TryParse<OrderStatus>(readModel.Status, out var parsed)
            ? parsed
            : OrderStatus.Pending;

        return new OrderAggregateState(
            readModel.OrderId,
            readModel.CartId,
            readModel.UserId,
            status,
            readModel.TotalAmount,
            readModel.Items,
            readModel.TransactionId,
            readModel.TrackingCode,
            readModel.FailureReason);
    }
}
