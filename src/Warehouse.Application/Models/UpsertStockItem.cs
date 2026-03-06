namespace Warehouse.Application;

public sealed record UpsertStockItem(Guid ProductId, string Sku, int AvailableQuantity);
