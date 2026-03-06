using Catalog.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Cqrs;
using Shared.BuildingBlocks.Http;

namespace Catalog.Api.Endpoints;

public static partial class CatalogEndpoints
{
    private static void MapBrandEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", GetBrands).WithName("GetBrands");
        group.MapGet("/{id:guid}", GetBrandById).WithName("GetBrandById");
        group.MapPost("/", CreateBrand).WithName("CreateBrand");
        group.MapPut("/{id:guid}", UpdateBrand).WithName("UpdateBrand");
        group.MapDelete("/{id:guid}", DeleteBrand).WithName("DeleteBrand");
    }

    private static async Task<Ok<IReadOnlyList<BrandView>>> GetBrands(IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var brands = await queryDispatcher.ExecuteAsync(new GetBrandsQuery(), cancellationToken);
        return TypedResults.Ok(brands);
    }

    private static async Task<Results<Ok<BrandView>, NotFound>> GetBrandById(Guid id, IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var brand = await queryDispatcher.ExecuteAsync(new GetBrandByIdQuery(id), cancellationToken);
        return brand is null ? TypedResults.NotFound() : TypedResults.Ok(brand);
    }

    private static async Task<Results<Created<BrandView>, ProblemHttpResult>> CreateBrand(
        CreateBrandCommand command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var errors = command.GetValidationErrors();
        if (errors.Count != 0)
        {
            return CreateValidationProblem(errors, "Invalid brand payload");
        }

        var brand = await commandDispatcher.ExecuteAsync(new CreateBrandCatalogCommand(command), cancellationToken);
        return TypedResults.Created($"/v1/brands/{brand.Id}", brand);
    }

    private static async Task<Results<Ok<BrandView>, NotFound, ProblemHttpResult>> UpdateBrand(
        Guid id,
        UpdateBrandCommand command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var errors = command.GetValidationErrors();
        if (errors.Count != 0)
        {
            return CreateValidationProblem(errors, "Invalid brand payload");
        }

        var brand = await commandDispatcher.ExecuteAsync(new UpdateBrandCatalogCommand(id, command), cancellationToken);
        return brand is null ? TypedResults.NotFound() : TypedResults.Ok(brand);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteBrand(Guid id, ICommandDispatcher commandDispatcher, CancellationToken cancellationToken)
    {
        var deleted = await commandDispatcher.ExecuteAsync(new DeleteBrandCatalogCommand(id), cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
