using Shared.BuildingBlocks.Contracts;

namespace Order.Domain;

public sealed record OrderPlacedDomain(Guid OrderId, Guid CartId, Guid UserId, IReadOnlyList<OrderItemDto> Items, decimal TotalAmount);
