using Catalog.Domain.Entities;

namespace Catalog.Application.Abstractions.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsBySlugAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> IsReferencedByProductsAsync(Guid id, CancellationToken cancellationToken);
    void Add(Category category);
    void Remove(Category category);
}
