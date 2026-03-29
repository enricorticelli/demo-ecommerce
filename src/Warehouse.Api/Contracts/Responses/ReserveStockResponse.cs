namespace Warehouse.Api.Contracts.Responses;

public sealed record ReserveStockResponse(Guid OrderId, bool Reserved, string? Reason);
