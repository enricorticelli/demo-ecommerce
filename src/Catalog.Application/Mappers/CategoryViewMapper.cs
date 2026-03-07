using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Mappers;

public sealed class CategoryViewMapper : IViewMapper<Category, CategoryView>
{
    public CategoryView Map(Category source)
    {
        return new CategoryView(source.Id, source.Name, source.Slug, source.Description);
    }
}
