using Shared.BuildingBlocks.Cqrs;

namespace Order.Application;

public sealed class CreateOrderCommandHandler(IOrderCommandService orderCommandService)
    : ICommandHandler<CreateOrderCommand, OrderCreationResult?>
{
    public Task<OrderCreationResult?> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        return orderCommandService.CreateOrderAsync(command, cancellationToken);
    }
}
