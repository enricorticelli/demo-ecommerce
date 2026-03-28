namespace Warehouse.Application.Commands;

public sealed record UpsertStockCommand(Guid ProductId, string Sku, int AvailableQuantity);
