using Order.Application;
using Order.Infrastructure.Persistence.ReadModels;

namespace Order.Infrastructure;

public sealed class OrderReadService(IOrderReadModelStore orderReadModelStore) : IOrderQueryService
{
    public async Task<OrderView?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var readModel = await orderReadModelStore.GetAsync(orderId, cancellationToken);
        return readModel is null
            ? null
            : new OrderView(
                readModel.OrderId,
                readModel.CartId,
                readModel.UserId,
                readModel.Status,
                readModel.TotalAmount,
                readModel.Items,
                readModel.TrackingCode,
                readModel.TransactionId,
                readModel.FailureReason);
    }
}
