using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Abstractions.Rules;
using Shared.BuildingBlocks.Exceptions;

namespace Catalog.Application.Services.Rules;

public sealed class CategoryRules(ICategoryRepository categoryRepository) : ICategoryRules
{
    public async Task EnsureSlugIsUniqueAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var exists = await categoryRepository.ExistsBySlugAsync(slug, excludingId, cancellationToken);
        if (exists)
        {
            throw new ConflictAppException($"Category slug '{slug}' already exists.");
        }
    }

    public async Task EnsureCanDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var isReferenced = await categoryRepository.IsReferencedByProductsAsync(id, cancellationToken);
        if (isReferenced)
        {
            throw new ConflictAppException($"Category '{id}' is referenced by at least one product.");
        }
    }
}
