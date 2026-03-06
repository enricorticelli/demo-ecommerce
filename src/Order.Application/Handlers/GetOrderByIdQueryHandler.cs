using Shared.BuildingBlocks.Cqrs;

namespace Order.Application;

public sealed class GetOrderByIdQueryHandler(IOrderService orderService)
    : IQueryHandler<GetOrderByIdQuery, OrderView?>
{
    public Task<OrderView?> HandleAsync(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        return orderService.GetOrderAsync(query.OrderId, cancellationToken);
    }
}
