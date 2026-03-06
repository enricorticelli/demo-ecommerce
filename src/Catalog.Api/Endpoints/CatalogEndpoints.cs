using Catalog.Api.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Api.Endpoints;

public static partial class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        MapProductEndpoints(app.MapGroup(CatalogRoutes.Products).WithTags("Catalog.Products").AddEndpointFilter<CqrsExceptionEndpointFilter>());
        MapBrandEndpoints(app.MapGroup(CatalogRoutes.Brands).WithTags("Catalog.Brands").AddEndpointFilter<CqrsExceptionEndpointFilter>());
        MapCategoryEndpoints(app.MapGroup(CatalogRoutes.Categories).WithTags("Catalog.Categories").AddEndpointFilter<CqrsExceptionEndpointFilter>());
        MapCollectionEndpoints(app.MapGroup(CatalogRoutes.Collections).WithTags("Catalog.Collections").AddEndpointFilter<CqrsExceptionEndpointFilter>());
        return app;
    }

    private static ProblemHttpResult CreateValidationProblem(Dictionary<string, string[]> errors, string detail)
    {
        return TypedResults.Problem(
            title: "Validation error",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?> { ["errors"] = errors });
    }
}
