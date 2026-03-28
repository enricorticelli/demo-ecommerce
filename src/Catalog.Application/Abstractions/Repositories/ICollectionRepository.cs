using Catalog.Domain.Entities;

namespace Catalog.Application.Abstractions.Repositories;

public interface ICollectionRepository
{
    Task<IReadOnlyList<Collection>> ListAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsBySlugAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> IsReferencedByProductsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistAllAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
    void Add(Collection collection);
    void Remove(Collection collection);
}
