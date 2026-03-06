namespace Warehouse.Api.Contracts;

public sealed record UpsertStockRequest(Guid ProductId, string Sku, int AvailableQuantity);
