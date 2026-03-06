using Catalog.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Cqrs;
using Shared.BuildingBlocks.Http;

namespace Catalog.Api.Endpoints;

public static partial class CatalogEndpoints
{
    private static void MapProductEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", GetProducts).WithName("GetProducts");
        group.MapGet("/new-arrivals", GetNewArrivals).WithName("GetNewArrivals");
        group.MapGet("/best-sellers", GetBestSellers).WithName("GetBestSellers");
        group.MapGet("/{id:guid}", GetProductById).WithName("GetProductById");
        group.MapPost("/", CreateProduct).WithName("CreateProduct");
        group.MapPut("/{id:guid}", UpdateProduct).WithName("UpdateProduct");
        group.MapDelete("/{id:guid}", DeleteProduct).WithName("DeleteProduct");
    }

    private static async Task<Ok<IReadOnlyList<ProductView>>> GetProducts(IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var products = await queryDispatcher.ExecuteAsync(new GetProductsQuery(), cancellationToken);
        return TypedResults.Ok(products);
    }

    private static async Task<Ok<IReadOnlyList<ProductView>>> GetNewArrivals(IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var products = await queryDispatcher.ExecuteAsync(new GetNewArrivalsQuery(), cancellationToken);
        return TypedResults.Ok(products);
    }

    private static async Task<Ok<IReadOnlyList<ProductView>>> GetBestSellers(IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var products = await queryDispatcher.ExecuteAsync(new GetBestSellersQuery(), cancellationToken);
        return TypedResults.Ok(products);
    }

    private static async Task<Results<Ok<ProductView>, NotFound>> GetProductById(Guid id, IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var product = await queryDispatcher.ExecuteAsync(new GetProductByIdQuery(id), cancellationToken);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    private static async Task<Results<Created<ProductView>, ProblemHttpResult>> CreateProduct(
        CreateProductCommand command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var errors = command.GetValidationErrors();
        if (errors.Count != 0)
        {
            return CreateValidationProblem(errors, "Invalid product payload");
        }

        var product = await commandDispatcher.ExecuteAsync(new CreateProductCatalogCommand(command), cancellationToken);
        if (product is null)
        {
            return TypedResults.Problem(
                title: "Invalid product references",
                detail: "Brand, category or collection reference does not exist.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return TypedResults.Created($"/v1/products/{product.Id}", product);
    }

    private static async Task<Results<Ok<ProductView>, NotFound, ProblemHttpResult>> UpdateProduct(
        Guid id,
        UpdateProductCommand command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var errors = command.GetValidationErrors();
        if (errors.Count != 0)
        {
            return CreateValidationProblem(errors, "Invalid product payload");
        }

        var product = await commandDispatcher.ExecuteAsync(new UpdateProductCatalogCommand(id, command), cancellationToken);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteProduct(Guid id, ICommandDispatcher commandDispatcher, CancellationToken cancellationToken)
    {
        var deleted = await commandDispatcher.ExecuteAsync(new DeleteProductCatalogCommand(id), cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
