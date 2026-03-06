namespace Order.Application;

public interface IOrderStateReader
{
    Task<OrderAggregateState?> GetAsync(Guid orderId, CancellationToken cancellationToken);
}
