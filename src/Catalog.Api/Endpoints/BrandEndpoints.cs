using Catalog.Api.Contracts;
using Catalog.Application.Brands;
using Catalog.Application.Commands;
using Catalog.Application.Queries;
using Catalog.Application.Views;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Cqrs.Abstractions;
using Shared.BuildingBlocks.Http;

namespace Catalog.Api.Endpoints;

public static class BrandEndpoints
{
    public static void MapBrandEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(CatalogRoutes.Brands)
            .WithTags("Catalog.Brands")
            .AddEndpointFilter<CqrsExceptionEndpointFilter>();
        
        group.MapGet("/", GetBrands).WithName("GetBrands");
        group.MapGet("/{id:guid}", GetBrandById).WithName("GetBrandById");
        group.MapPost("/", CreateBrand).WithName("CreateBrand");
        group.MapPut("/{id:guid}", UpdateBrand).WithName("UpdateBrand");
        group.MapDelete("/{id:guid}", DeleteBrand).WithName("DeleteBrand");
    }

    private static async Task<Ok<IReadOnlyList<BrandView>>> GetBrands(
        IQueryDispatcher queryDispatcher,
        int? limit,
        int? offset,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit ?? 200, 1, 200);
        var safeOffset = Math.Max(offset ?? 0, 0);
        var brands = await queryDispatcher.ExecuteAsync(new GetBrandsQuery(safeLimit, safeOffset), cancellationToken);
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
            return EndpointsHelpers.CreateValidationProblem(errors, "Invalid brand payload");
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
            return EndpointsHelpers.CreateValidationProblem(errors, "Invalid brand payload");
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
