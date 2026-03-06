using Shared.BuildingBlocks.Cqrs;

namespace Order.Application;

public sealed class GetOrderByIdQueryHandler(IOrderQueryService orderQueryService)
    : IQueryHandler<GetOrderByIdQuery, OrderView?>
{
    public Task<OrderView?> HandleAsync(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        return orderQueryService.GetOrderAsync(query.OrderId, cancellationToken);
    }
}
