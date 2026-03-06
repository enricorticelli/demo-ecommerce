namespace Order.Application;

public interface IOrderCommandService
{
    Task<OrderCreationResult?> CreateOrderAsync(CreateOrderCommand command, CancellationToken cancellationToken);
}
