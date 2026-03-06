using Shared.BuildingBlocks.Cqrs;

namespace Order.Application;

public sealed class GetOrdersQueryHandler(IOrderQueryService orderQueryService)
    : IQueryHandler<GetOrdersQuery, IReadOnlyList<OrderView>>
{
    public Task<IReadOnlyList<OrderView>> HandleAsync(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        return orderQueryService.GetOrdersAsync(query.Limit, cancellationToken);
    }
}
