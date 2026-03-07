using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Abstractions.Rules;
using Shared.BuildingBlocks.Exceptions;

namespace Catalog.Application.Services.Rules;

public sealed class CollectionRules(ICollectionRepository collectionRepository) : ICollectionRules
{
    public async Task EnsureSlugIsUniqueAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var exists = await collectionRepository.ExistsBySlugAsync(slug, excludingId, cancellationToken);
        if (exists)
        {
            throw new ConflictAppException($"Collection slug '{slug}' already exists.");
        }
    }

    public async Task EnsureCanDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var isReferenced = await collectionRepository.IsReferencedByProductsAsync(id, cancellationToken);
        if (isReferenced)
        {
            throw new ConflictAppException($"Collection '{id}' is referenced by at least one product.");
        }
    }

    public async Task EnsureCollectionsExistAsync(IReadOnlyList<Guid> collectionIds, CancellationToken cancellationToken)
    {
        var normalizedCollectionIds = collectionIds.Distinct().ToArray();
        var exists = await collectionRepository.ExistAllAsync(normalizedCollectionIds, cancellationToken);
        if (!exists)
        {
            throw new ValidationAppException("One or more collections do not exist.");
        }
    }
}
