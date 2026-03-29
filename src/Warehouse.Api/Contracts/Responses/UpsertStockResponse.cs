namespace Warehouse.Api.Contracts.Responses;

public sealed record UpsertStockResponse(Guid ProductId, string Sku, int AvailableQuantity);
