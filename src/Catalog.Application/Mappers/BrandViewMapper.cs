using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Mappers;

public sealed class BrandViewMapper : IViewMapper<Brand, BrandView>
{
    public BrandView Map(Brand source)
    {
        return new BrandView(source.Id, source.Name, source.Slug, source.Description);
    }
}
