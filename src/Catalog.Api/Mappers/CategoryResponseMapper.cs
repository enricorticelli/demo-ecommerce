using Catalog.Api.Contracts.Responses;
using Catalog.Application.Views;

namespace Catalog.Api.Mappers;

public static class CategoryResponseMapper
{
    public static CategoryResponse ToResponse(this CategoryView view)
    {
        return new CategoryResponse(view.Id, view.Name, view.Slug, view.Description);
    }
}
