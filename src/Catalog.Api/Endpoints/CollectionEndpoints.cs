using Catalog.Api.Contracts;
using Catalog.Api.Contracts.Requests;
using Catalog.Api.Mappers;
using Catalog.Application.Abstractions.Commands;
using Catalog.Application.Abstractions.Queries;
using Shared.BuildingBlocks.Api.Correlation;
using Shared.BuildingBlocks.Api.Errors;
using Shared.BuildingBlocks.Api.Pagination;

namespace Catalog.Api.Endpoints;

public static class CollectionEndpoints
{
    public static RouteGroupBuilder MapCollectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(CatalogRoutes.StoreCollections)
            .WithTags("Catalog");

        group.MapGet("/", GetCollections).WithName("StoreGetCollections");
        group.MapGet("/{id:guid}", GetCollectionById).WithName("StoreGetCollectionById");
        group.MapPost("/", CreateCollection).WithName("StoreCreateCollection");
        group.MapPut("/{id:guid}", UpdateCollection).WithName("StoreUpdateCollection");
        group.MapDelete("/{id:guid}", DeleteCollection).WithName("StoreDeleteCollection");

        return group;
    }

    private static async Task<IResult> GetCollections(
        int? limit,
        int? offset,
        string? searchTerm,
        ICollectionQueryService service,
        CancellationToken cancellationToken)
    {
        var (normalizedLimit, normalizedOffset) = PaginationNormalizer.Normalize(limit, offset);
        var collections = await service.ListAsync(searchTerm, cancellationToken);
        return Results.Ok(collections
            .Skip(normalizedOffset)
            .Take(normalizedLimit)
            .Select(x => x.ToResponse()));
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
            return Results.Created($"{CatalogRoutes.StoreCollections}/{collection.Id}", response);
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
