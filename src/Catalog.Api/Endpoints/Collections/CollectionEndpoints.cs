using Catalog.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Cqrs;
using Shared.BuildingBlocks.Http;

namespace Catalog.Api.Endpoints;

public static partial class CatalogEndpoints
{
    private static void MapCollectionEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", GetCollections).WithName("GetCollections");
        group.MapGet("/{id:guid}", GetCollectionById).WithName("GetCollectionById");
        group.MapPost("/", CreateCollection).WithName("CreateCollection");
        group.MapPut("/{id:guid}", UpdateCollection).WithName("UpdateCollection");
        group.MapDelete("/{id:guid}", DeleteCollection).WithName("DeleteCollection");
    }

    private static async Task<Ok<IReadOnlyList<CollectionView>>> GetCollections(IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var collections = await queryDispatcher.ExecuteAsync(new GetCollectionsQuery(), cancellationToken);
        return TypedResults.Ok(collections);
    }

    private static async Task<Results<Ok<CollectionView>, NotFound>> GetCollectionById(Guid id, IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var collection = await queryDispatcher.ExecuteAsync(new GetCollectionByIdQuery(id), cancellationToken);
        return collection is null ? TypedResults.NotFound() : TypedResults.Ok(collection);
    }

    private static async Task<Results<Created<CollectionView>, ProblemHttpResult>> CreateCollection(
        CreateCollectionCommand command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var errors = command.GetValidationErrors();
        if (errors.Count != 0)
        {
            return CreateValidationProblem(errors, "Invalid collection payload");
        }

        var collection = await commandDispatcher.ExecuteAsync(new CreateCollectionCatalogCommand(command), cancellationToken);
        return TypedResults.Created($"/v1/collections/{collection.Id}", collection);
    }

    private static async Task<Results<Ok<CollectionView>, NotFound, ProblemHttpResult>> UpdateCollection(
        Guid id,
        UpdateCollectionCommand command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var errors = command.GetValidationErrors();
        if (errors.Count != 0)
        {
            return CreateValidationProblem(errors, "Invalid collection payload");
        }

        var collection = await commandDispatcher.ExecuteAsync(new UpdateCollectionCatalogCommand(id, command), cancellationToken);
        return collection is null ? TypedResults.NotFound() : TypedResults.Ok(collection);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteCollection(Guid id, ICommandDispatcher commandDispatcher, CancellationToken cancellationToken)
    {
        var deleted = await commandDispatcher.ExecuteAsync(new DeleteCollectionCatalogCommand(id), cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
