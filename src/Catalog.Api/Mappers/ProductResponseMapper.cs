using Catalog.Api.Contracts.Responses;
using Catalog.Application.Views;

namespace Catalog.Api.Mappers;

public static class ProductResponseMapper
{
    public static ProductResponse ToResponse(this ProductView view)
    {
        return new ProductResponse(
            view.Id,
            view.Sku,
            view.Name,
            view.Description,
            view.Price,
            view.BrandId,
            view.BrandName,
            view.CategoryId,
            view.CategoryName,
            view.CollectionIds,
            view.CollectionNames,
            view.IsNewArrival,
            view.IsBestSeller,
            view.CreatedAtUtc);
    }
}
