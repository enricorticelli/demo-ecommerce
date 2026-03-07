using Catalog.Application.Views;

namespace Catalog.Application.Abstractions.Queries;

public interface ICategoryQueryService
{
    Task<IReadOnlyList<CategoryView>> ListAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<CategoryView> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
