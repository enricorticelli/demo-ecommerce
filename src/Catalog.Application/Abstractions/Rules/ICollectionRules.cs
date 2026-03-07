namespace Catalog.Application.Abstractions.Rules;

public interface ICollectionRules
{
    Task EnsureSlugIsUniqueAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task EnsureCanDeleteAsync(Guid id, CancellationToken cancellationToken);
    Task EnsureCollectionsExistAsync(IReadOnlyList<Guid> collectionIds, CancellationToken cancellationToken);
}
