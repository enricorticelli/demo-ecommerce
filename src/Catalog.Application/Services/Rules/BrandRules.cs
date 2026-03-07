using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Abstractions.Rules;
using Shared.BuildingBlocks.Exceptions;

namespace Catalog.Application.Services.Rules;

public sealed class BrandRules(IBrandRepository brandRepository) : IBrandRules
{
    public async Task EnsureSlugIsUniqueAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var exists = await brandRepository.ExistsBySlugAsync(slug, excludingId, cancellationToken);
        if (exists)
        {
            throw new ConflictAppException($"Brand slug '{slug}' already exists.");
        }
    }

    public async Task EnsureCanDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var isReferenced = await brandRepository.IsReferencedByProductsAsync(id, cancellationToken);
        if (isReferenced)
        {
            throw new ConflictAppException($"Brand '{id}' is referenced by at least one product.");
        }
    }
}
