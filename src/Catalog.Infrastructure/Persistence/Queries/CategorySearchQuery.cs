using Catalog.Domain.Entities;

namespace Catalog.Infrastructure.Persistence.Queries;

public static class CategorySearchQuery
{
    public static IQueryable<Category> Apply(IQueryable<Category> query, string? searchTerm)
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
