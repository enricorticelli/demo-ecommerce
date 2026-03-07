using Catalog.Domain.Entities;

namespace Catalog.Infrastructure.Persistence.Queries;

public static class CollectionSearchQuery
{
    public static IQueryable<CatalogCollection> Apply(IQueryable<CatalogCollection> query, string? searchTerm)
    {
        var normalizedSearch = searchTerm?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return query;
        }

        return query.Where(x =>
            x.Name.ToLower().Contains(normalizedSearch) ||
            x.Slug.ToLower().Contains(normalizedSearch) ||
            x.Description.ToLower().Contains(normalizedSearch));
    }
}
