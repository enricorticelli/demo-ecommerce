using System.ComponentModel.DataAnnotations;

namespace Warehouse.Api.Contracts.Requests;

public sealed record UpsertStockRequest(
    Guid ProductId,
    [property: Required, StringLength(64)] string Sku,
    [property: Range(0, int.MaxValue)] int AvailableQuantity);
