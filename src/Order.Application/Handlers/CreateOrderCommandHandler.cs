using Shared.BuildingBlocks.Cqrs;

namespace Order.Application;

public sealed class CreateOrderCommandHandler(IOrderService orderService)
    : ICommandHandler<CreateOrderCommand, OrderCreationResult?>
{
    public Task<OrderCreationResult?> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        return orderService.CreateOrderAsync(command, cancellationToken);
    }
}
