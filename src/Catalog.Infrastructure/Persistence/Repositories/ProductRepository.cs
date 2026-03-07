using Catalog.Application.Abstractions.Repositories;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence.Queries;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(CatalogDbContext dbContext) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> ListAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = ProductSearchQuery.Apply(GetBaseListQuery(), searchTerm);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListNewArrivalsAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = ProductSearchQuery.Apply(GetBaseListQuery().Where(x => x.IsNewArrival), searchTerm);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListBestSellersAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = ProductSearchQuery.Apply(GetBaseListQuery().Where(x => x.IsBestSeller), searchTerm);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return GetBaseListQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Product?> GetByIdWithCollectionsAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Products
            .Include(x => x.ProductCollections)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsBySkuAsync(string sku, Guid? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.Products.AnyAsync(x => x.Sku == sku && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
    }

    public void Add(Product product)
    {
        dbContext.Products.Add(product);
    }

    public void Remove(Product product)
    {
        dbContext.ProductCollections.RemoveRange(product.ProductCollections);
        dbContext.Products.Remove(product);
    }

    public void ReplaceCollections(Product product, IReadOnlyList<Guid> collectionIds)
    {
        dbContext.ProductCollections.RemoveRange(product.ProductCollections);
        product.ProductCollections.Clear();

        foreach (var collectionId in collectionIds)
        {
            var link = new ProductCollection { ProductId = product.Id, CollectionId = collectionId };
            product.ProductCollections.Add(link);
            dbContext.ProductCollections.Add(link);
        }
    }

    private IQueryable<Product> GetBaseListQuery()
    {
        return dbContext.Products
            .Include(x => x.Brand)
            .Include(x => x.Category)
            .Include(x => x.ProductCollections)
            .ThenInclude(x => x.Collection)
            .AsQueryable();
    }
}
