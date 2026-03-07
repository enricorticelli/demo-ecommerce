using Catalog.Api.Contracts.Responses;
using Catalog.Application.Views;

namespace Catalog.Api.Mappers;

public static class CollectionResponseMapper
{
    public static CollectionResponse ToResponse(this CollectionView view)
    {
        return new CollectionResponse(view.Id, view.Name, view.Slug, view.Description, view.IsFeatured);
    }
}
