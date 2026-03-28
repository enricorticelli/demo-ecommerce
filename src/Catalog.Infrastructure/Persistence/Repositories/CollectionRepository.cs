using Catalog.Application.Abstractions.Repositories;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence.Queries;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

public sealed class CollectionRepository(CatalogDbContext dbContext) : ICollectionRepository
{
    public async Task<IReadOnlyList<Collection>> ListAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = CollectionSearchQuery.Apply(dbContext.Collections.AsNoTracking(), searchTerm);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Collections.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsBySlugAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.Collections.AnyAsync(x => x.Slug == slug && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
    }

    public Task<bool> IsReferencedByProductsAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.ProductCollections.AnyAsync(x => x.CollectionId == id, cancellationToken);
    }

    public async Task<bool> ExistAllAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return true;
        }

        var count = await dbContext.Collections.CountAsync(x => ids.Contains(x.Id), cancellationToken);
        return count == ids.Count;
    }

    public void Add(Collection collection)
    {
        dbContext.Collections.Add(collection);
    }

    public void Remove(Collection collection)
    {
        dbContext.Collections.Remove(collection);
    }
}
