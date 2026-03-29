using System.ComponentModel.DataAnnotations;

namespace Catalog.Api.Contracts.Requests;

public sealed record UpdateProductRequest(
    [property: Required, StringLength(64)] string Sku,
    [property: Required, StringLength(256)] string Name,
    [property: StringLength(1024)] string Description,
    [property: Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal Price,
    Guid BrandId,
    Guid CategoryId,
    [property: Required] IReadOnlyList<Guid> CollectionIds,
    bool IsNewArrival,
    bool IsBestSeller);
