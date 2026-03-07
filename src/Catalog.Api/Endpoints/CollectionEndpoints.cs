using Catalog.Api.Contracts;
using Catalog.Api.Contracts.Requests;
using Catalog.Api.Contracts.Responses;
using Catalog.Api.Mappers;
using Catalog.Application.Abstractions.Commands;
using Catalog.Application.Abstractions.Queries;
using Shared.BuildingBlocks.Api.Correlation;
using Shared.BuildingBlocks.Api.Errors;

namespace Catalog.Api.Endpoints;

public static class CollectionEndpoints
{
    public static RouteGroupBuilder MapCollectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(CatalogRoutes.Collections)
            .WithTags("Catalog");

        group.MapGet("/", GetCollections).WithName("GetCollections");
        group.MapGet("/{id:guid}", GetCollectionById).WithName("GetCollectionById");
        group.MapPost("/", CreateCollection).WithName("CreateCollection");
        group.MapPut("/{id:guid}", UpdateCollection).WithName("UpdateCollection");
        group.MapDelete("/{id:guid}", DeleteCollection).WithName("DeleteCollection");

        return group;
    }

    private static async Task<IResult> GetCollections(string? searchTerm, ICollectionQueryService service, CancellationToken cancellationToken)
    {
        var collections = await service.ListAsync(searchTerm, cancellationToken);
        return Results.Ok(collections.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> GetCollectionById(Guid id, ICollectionQueryService service, CancellationToken cancellationToken)
    {
        try
        {
            var collection = await service.GetByIdAsync(id, cancellationToken);
            return Results.Ok(collection.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> CreateCollection(CreateCollectionRequest request, ICollectionCommandService service, HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var collection = await service.CreateAsync(
                request.Name,
                request.Slug,
                request.Description,
                request.IsFeatured,
                correlationId,
                cancellationToken);

            var response = collection.ToResponse();
            return Results.Created($"{CatalogRoutes.Collections}/{collection.Id}", response);
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> UpdateCollection(Guid id, UpdateCollectionRequest request, ICollectionCommandService service, HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var collection = await service.UpdateAsync(
                id,
                request.Name,
                request.Slug,
                request.Description,
                request.IsFeatured,
                correlationId,
                cancellationToken);

            return Results.Ok(collection.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> DeleteCollection(Guid id, ICollectionCommandService service, HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            await service.DeleteAsync(id, correlationId, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }
}
