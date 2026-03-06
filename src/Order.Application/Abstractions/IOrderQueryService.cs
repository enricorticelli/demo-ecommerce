namespace Order.Application;

public interface IOrderQueryService
{
    Task<OrderView?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);
}
