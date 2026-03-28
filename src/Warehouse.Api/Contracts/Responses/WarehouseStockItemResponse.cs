namespace Warehouse.Api.Contracts.Responses;

public sealed record WarehouseStockItemResponse(Guid ProductId, string Sku, int AvailableQuantity);
