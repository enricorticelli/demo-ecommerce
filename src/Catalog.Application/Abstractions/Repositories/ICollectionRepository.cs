using Catalog.Domain.Entities;

namespace Catalog.Application.Abstractions.Repositories;

public interface ICollectionRepository
{
    Task<IReadOnlyList<CatalogCollection>> ListAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<CatalogCollection?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsBySlugAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> IsReferencedByProductsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistAllAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
    void Add(CatalogCollection collection);
    void Remove(CatalogCollection collection);
}
