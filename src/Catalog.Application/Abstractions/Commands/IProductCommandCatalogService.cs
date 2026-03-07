using Catalog.Application.Views;

namespace Catalog.Application.Abstractions.Commands;

public interface IProductCommandCatalogService
{
    Task<ProductView> CreateAsync(string sku, string name, string description, decimal price, Guid brandId, Guid categoryId,
        IReadOnlyList<Guid> collectionIds, bool isNewArrival, bool isBestSeller, string correlationId,
        CancellationToken cancellationToken);

    Task<ProductView> UpdateAsync(Guid id, string sku, string name, string description, decimal price, Guid brandId,
        Guid categoryId, IReadOnlyList<Guid> collectionIds, bool isNewArrival, bool isBestSeller,
        string correlationId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, string correlationId, CancellationToken cancellationToken);
}
