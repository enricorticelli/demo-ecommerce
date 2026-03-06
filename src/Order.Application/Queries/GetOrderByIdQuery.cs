using Shared.BuildingBlocks.Cqrs;

namespace Order.Application;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderView?>;
