namespace Warehouse.Api.Contracts.Requests;

public sealed record ReserveStockItemRequest(Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice);
