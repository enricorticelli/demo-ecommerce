using Catalog.Application.Views;

namespace Catalog.Application.Abstractions.Queries;

public interface IBrandQueryService
{
    Task<IReadOnlyList<BrandView>> ListAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<BrandView> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
