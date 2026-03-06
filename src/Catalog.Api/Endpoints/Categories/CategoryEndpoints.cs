using Catalog.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Cqrs;
using Shared.BuildingBlocks.Http;

namespace Catalog.Api.Endpoints;

public static partial class CatalogEndpoints
{
    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", GetCategories).WithName("GetCategories");
        group.MapGet("/{id:guid}", GetCategoryById).WithName("GetCategoryById");
        group.MapPost("/", CreateCategory).WithName("CreateCategory");
        group.MapPut("/{id:guid}", UpdateCategory).WithName("UpdateCategory");
        group.MapDelete("/{id:guid}", DeleteCategory).WithName("DeleteCategory");
    }

    private static async Task<Ok<IReadOnlyList<CategoryView>>> GetCategories(IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var categories = await queryDispatcher.ExecuteAsync(new GetCategoriesQuery(), cancellationToken);
        return TypedResults.Ok(categories);
    }

    private static async Task<Results<Ok<CategoryView>, NotFound>> GetCategoryById(Guid id, IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var category = await queryDispatcher.ExecuteAsync(new GetCategoryByIdQuery(id), cancellationToken);
        return category is null ? TypedResults.NotFound() : TypedResults.Ok(category);
    }

    private static async Task<Results<Created<CategoryView>, ProblemHttpResult>> CreateCategory(
        CreateCategoryCommand command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var errors = command.GetValidationErrors();
        if (errors.Count != 0)
        {
            return CreateValidationProblem(errors, "Invalid category payload");
        }

        var category = await commandDispatcher.ExecuteAsync(new CreateCategoryCatalogCommand(command), cancellationToken);
        return TypedResults.Created($"/v1/categories/{category.Id}", category);
    }

    private static async Task<Results<Ok<CategoryView>, NotFound, ProblemHttpResult>> UpdateCategory(
        Guid id,
        UpdateCategoryCommand command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var errors = command.GetValidationErrors();
        if (errors.Count != 0)
        {
            return CreateValidationProblem(errors, "Invalid category payload");
        }

        var category = await commandDispatcher.ExecuteAsync(new UpdateCategoryCatalogCommand(id, command), cancellationToken);
        return category is null ? TypedResults.NotFound() : TypedResults.Ok(category);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteCategory(Guid id, ICommandDispatcher commandDispatcher, CancellationToken cancellationToken)
    {
        var deleted = await commandDispatcher.ExecuteAsync(new DeleteCategoryCatalogCommand(id), cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
