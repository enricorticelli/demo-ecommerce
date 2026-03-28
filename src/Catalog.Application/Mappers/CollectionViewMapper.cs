using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Mappers;

public sealed class CollectionViewMapper : IViewMapper<Collection, CollectionView>
{
    public CollectionView Map(Collection source)
    {
        return new CollectionView(source.Id, source.Name, source.Slug, source.Description, source.IsFeatured);
    }
}
