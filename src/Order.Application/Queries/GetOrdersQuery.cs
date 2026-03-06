using Shared.BuildingBlocks.Cqrs;

namespace Order.Application;

public sealed record GetOrdersQuery(int Limit) : IQuery<IReadOnlyList<OrderView>>;
