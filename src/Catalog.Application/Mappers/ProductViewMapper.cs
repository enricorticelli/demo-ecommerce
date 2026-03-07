using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Mappers;

public sealed class ProductViewMapper : IViewMapper<Product, ProductView>
{
    public ProductView Map(Product source)
    {
        var collectionIds = source.ProductCollections.Select(x => x.CollectionId).ToArray();
        var collectionNames = source.ProductCollections.Select(x => x.Collection.Name).ToArray();

        return new ProductView(
            source.Id,
            source.Sku,
            source.Name,
            source.Description,
            source.Price,
            source.BrandId,
            source.Brand.Name,
            source.CategoryId,
            source.Category.Name,
            collectionIds,
            collectionNames,
            source.IsNewArrival,
            source.IsBestSeller,
            source.CreatedAtUtc);
    }
}
