using Catalog.Application.Abstractions.Repositories;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence.Queries;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

public sealed class BrandRepository(CatalogDbContext dbContext) : IBrandRepository
{
    public async Task<IReadOnlyList<Brand>> ListAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = BrandSearchQuery.Apply(dbContext.Brands.AsNoTracking(), searchTerm);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Brands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsBySlugAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.Brands.AnyAsync(x => x.Slug == slug && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
    }

    public Task<bool> IsReferencedByProductsAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Products.AnyAsync(x => x.BrandId == id, cancellationToken);
    }

    public void Add(Brand brand)
    {
        dbContext.Brands.Add(brand);
    }

    public void Remove(Brand brand)
    {
        dbContext.Brands.Remove(brand);
    }
}
