using Catalog.Domain.Entities;

namespace Catalog.Application.Abstractions.Repositories;

public interface IBrandRepository
{
    Task<IReadOnlyList<Brand>> ListAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsBySlugAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> IsReferencedByProductsAsync(Guid id, CancellationToken cancellationToken);
    void Add(Brand brand);
    void Remove(Brand brand);
}
