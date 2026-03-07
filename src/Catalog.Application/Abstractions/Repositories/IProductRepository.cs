using Catalog.Domain.Entities;

namespace Catalog.Application.Abstractions.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> ListAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> ListNewArrivalsAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> ListBestSellersAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Product?> GetByIdWithCollectionsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsBySkuAsync(string sku, Guid? excludingId, CancellationToken cancellationToken);
    void Add(Product product);
    void Remove(Product product);
    void ReplaceCollections(Product product, IReadOnlyList<Guid> collectionIds);
}
