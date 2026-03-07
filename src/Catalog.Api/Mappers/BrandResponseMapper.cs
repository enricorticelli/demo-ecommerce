using Catalog.Api.Contracts.Responses;
using Catalog.Application.Views;

namespace Catalog.Api.Mappers;

public static class BrandResponseMapper
{
    public static BrandResponse ToResponse(this BrandView view)
    {
        return new BrandResponse(view.Id, view.Name, view.Slug, view.Description);
    }
}
