namespace Warehouse.Api.Contracts.Requests;

public sealed record ReserveStockRequest(Guid OrderId, IReadOnlyList<ReserveStockItemRequest> Items);
