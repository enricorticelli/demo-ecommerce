using Catalog.Application.Views;

namespace Catalog.Application.Abstractions.Queries;

public interface ICollectionQueryService
{
    Task<IReadOnlyList<CollectionView>> ListAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<CollectionView> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
