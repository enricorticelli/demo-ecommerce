namespace Catalog.Api.Contracts.Responses;

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    Guid BrandId,
    string BrandName,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<Guid> CollectionIds,
    IReadOnlyList<string> CollectionNames,
    bool IsNewArrival,
    bool IsBestSeller,
    DateTimeOffset CreatedAtUtc);
