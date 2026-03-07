using Catalog.Domain.Entities;

namespace Catalog.Infrastructure.Persistence.Queries;

public static class ProductSearchQuery
{
    public static IQueryable<Product> Apply(IQueryable<Product> query, string? searchTerm)
    {
        var normalizedSearch = searchTerm?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return query;
        }

        return query.Where(x =>
            x.Sku.ToLower().Contains(normalizedSearch) ||
            x.Name.ToLower().Contains(normalizedSearch) ||
            x.Description.ToLower().Contains(normalizedSearch) ||
            x.Brand.Name.ToLower().Contains(normalizedSearch) ||
            x.Category.Name.ToLower().Contains(normalizedSearch) ||
            x.ProductCollections.Any(pc => pc.Collection.Name.ToLower().Contains(normalizedSearch)));
    }
}
