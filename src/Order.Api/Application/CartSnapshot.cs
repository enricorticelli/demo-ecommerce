using Shared.BuildingBlocks.Contracts;

namespace Order.Api.Application;

public sealed record CartSnapshot(Guid CartId, Guid UserId, IReadOnlyList<OrderItemDto> Items, decimal TotalAmount);
