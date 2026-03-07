using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Mappers;

public sealed class CollectionViewMapper : IViewMapper<CatalogCollection, CollectionView>
{
    public CollectionView Map(CatalogCollection source)
    {
        return new CollectionView(source.Id, source.Name, source.Slug, source.Description, source.IsFeatured);
    }
}
