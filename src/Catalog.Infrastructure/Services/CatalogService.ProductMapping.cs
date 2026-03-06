using Catalog.Application;
using Catalog.Domain;
using Marten;

namespace Catalog.Infrastructure;

public sealed partial class CatalogService
{
    private async Task<bool> ReferencesExistAsync(Guid brandId, Guid categoryId, IReadOnlyList<Guid> collectionIds, CancellationToken cancellationToken)
    {
        var brand = await _querySession.LoadAsync<BrandAggregate>(brandId, cancellationToken);
        var category = await _querySession.LoadAsync<CategoryAggregate>(categoryId, cancellationToken);
        if (brand is null || category is null)
        {
            return false;
        }

        if (collectionIds.Count == 0)
        {
            return true;
        }

        var existingCollectionIds = await _querySession.Query<CollectionAggregate>()
            .Where(x => collectionIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return collectionIds.Distinct().All(existingCollectionIds.Contains);
    }

    private async Task<IReadOnlyList<ProductView>> BuildProductViewsAsync(IReadOnlyList<ProductAggregate> products, CancellationToken cancellationToken)
    {
        if (products.Count == 0)
        {
            return [];
        }

        var brandIds = products.Select(p => p.BrandId).Distinct().ToArray();
        var categoryIds = products.Select(p => p.CategoryId).Distinct().ToArray();
        var collectionIds = products.SelectMany(p => p.CollectionIds).Distinct().ToArray();

        var brands = await _querySession.Query<BrandAggregate>()
            .Where(x => brandIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var categories = await _querySession.Query<CategoryAggregate>()
            .Where(x => categoryIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var collections = await _querySession.Query<CollectionAggregate>()
            .Where(x => collectionIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var brandMap = brands.ToDictionary(x => x.Id, x => x);
        var categoryMap = categories.ToDictionary(x => x.Id, x => x);
        var collectionMap = collections.ToDictionary(x => x.Id, x => x);

        return products.Select(p => MapToView(p, brandMap, categoryMap, collectionMap)).ToArray();
    }

    private async Task<ProductView> BuildProductViewAsync(ProductAggregate product, CancellationToken cancellationToken)
    {
        var brands = await _querySession.Query<BrandAggregate>()
            .Where(x => x.Id == product.BrandId)
            .ToListAsync(cancellationToken);

        var categories = await _querySession.Query<CategoryAggregate>()
            .Where(x => x.Id == product.CategoryId)
            .ToListAsync(cancellationToken);

        var collections = await _querySession.Query<CollectionAggregate>()
            .Where(x => product.CollectionIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        return MapToView(
            product,
            brands.ToDictionary(x => x.Id, x => x),
            categories.ToDictionary(x => x.Id, x => x),
            collections.ToDictionary(x => x.Id, x => x));
    }

    private static ProductView MapToView(
        ProductAggregate product,
        IReadOnlyDictionary<Guid, BrandAggregate> brandMap,
        IReadOnlyDictionary<Guid, CategoryAggregate> categoryMap,
        IReadOnlyDictionary<Guid, CollectionAggregate> collectionMap)
    {
        var brandName = brandMap.TryGetValue(product.BrandId, out var brand) ? brand.Name : "Unknown brand";
        var categoryName = categoryMap.TryGetValue(product.CategoryId, out var category) ? category.Name : "Unknown category";

        var collectionNames = product.CollectionIds
            .Where(collectionMap.ContainsKey)
            .Select(id => collectionMap[id].Name)
            .ToArray();

        return new ProductView(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.Price,
            product.BrandId,
            brandName,
            product.CategoryId,
            categoryName,
            product.CollectionIds,
            collectionNames,
            product.IsNewArrival,
            product.IsBestSeller,
            product.CreatedAtUtc);
    }
}
