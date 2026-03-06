namespace Order.Application;

public interface IOrderQueryService
{
    Task<OrderView?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderView>> GetOrdersAsync(int limit, CancellationToken cancellationToken);
}
