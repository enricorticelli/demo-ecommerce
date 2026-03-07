using Catalog.Application.Abstractions.Repositories;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence.Queries;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(CatalogDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = CategorySearchQuery.Apply(dbContext.Categories.AsNoTracking(), searchTerm);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsBySlugAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.Categories.AnyAsync(x => x.Slug == slug && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
    }

    public Task<bool> IsReferencedByProductsAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Products.AnyAsync(x => x.CategoryId == id, cancellationToken);
    }

    public void Add(Category category)
    {
        dbContext.Categories.Add(category);
    }

    public void Remove(Category category)
    {
        dbContext.Categories.Remove(category);
    }
}
