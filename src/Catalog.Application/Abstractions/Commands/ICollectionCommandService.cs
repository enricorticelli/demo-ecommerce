using Catalog.Application.Views;

namespace Catalog.Application.Abstractions.Commands;

public interface ICollectionCommandService
{
    Task<CollectionView> CreateAsync(string name, string slug, string description, bool isFeatured, string correlationId, CancellationToken cancellationToken);
    Task<CollectionView> UpdateAsync(Guid id, string name, string slug, string description, bool isFeatured, string correlationId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, string correlationId, CancellationToken cancellationToken);
}
