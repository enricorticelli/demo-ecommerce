using Shared.BuildingBlocks.Contracts;

namespace Order.Infrastructure.Persistence.ReadModels;

public sealed record OrderReadModelRow(
    Guid OrderId,
    Guid CartId,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    IReadOnlyList<OrderItemDto> Items,
    string TransactionId,
    string TrackingCode,
    string FailureReason);
