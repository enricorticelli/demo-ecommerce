namespace Warehouse.Api.Contracts.Requests;

public sealed record GetStockByProductsRequest(
    IReadOnlyList<Guid> ProductIds,
    int? LowStockThreshold);
