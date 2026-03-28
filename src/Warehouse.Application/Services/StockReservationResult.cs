namespace Warehouse.Application.Services;

public sealed record StockReservationResult(Guid OrderId, bool IsReserved, string? FailureReason);
