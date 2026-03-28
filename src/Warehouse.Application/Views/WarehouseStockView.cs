namespace Warehouse.Application.Views;

public sealed record WarehouseStockView(Guid ProductId, string Sku, int AvailableQuantity);
