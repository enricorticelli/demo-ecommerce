namespace Warehouse.Api.Contracts.Responses;

public sealed record GetStockByProductsResponse(IReadOnlyList<WarehouseStockItemResponse> Items);
