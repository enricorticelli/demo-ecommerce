using Catalog.Api.Contracts;
using Catalog.Api.Contracts.Requests;
using Catalog.Api.Contracts.Responses;
using Catalog.Api.Mappers;
using Catalog.Application.Abstractions.Commands;
using Catalog.Application.Abstractions.Queries;
using Shared.BuildingBlocks.Api.Correlation;
using Shared.BuildingBlocks.Api.Errors;

namespace Catalog.Api.Endpoints;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(CatalogRoutes.Categories)
            .WithTags("Catalog");

        group.MapGet("/", GetCategories).WithName("GetCategories");
        group.MapGet("/{id:guid}", GetCategoryById).WithName("GetCategoryById");
        group.MapPost("/", CreateCategory).WithName("CreateCategory");
        group.MapPut("/{id:guid}", UpdateCategory).WithName("UpdateCategory");
        group.MapDelete("/{id:guid}", DeleteCategory).WithName("DeleteCategory");

        return group;
    }

    private static async Task<IResult> GetCategories(string? searchTerm, ICategoryQueryService service, CancellationToken cancellationToken)
    {
        var categories = await service.ListAsync(searchTerm, cancellationToken);
        return Results.Ok(categories.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> GetCategoryById(Guid id, ICategoryQueryService service, CancellationToken cancellationToken)
    {
        try
        {
            var category = await service.GetByIdAsync(id, cancellationToken);
            return Results.Ok(category.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> CreateCategory(CreateCategoryRequest request, ICategoryCommandService service, HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var category = await service.CreateAsync(request.Name, request.Slug, request.Description, correlationId, cancellationToken);
            var response = category.ToResponse();
            return Results.Created($"{CatalogRoutes.Categories}/{category.Id}", response);
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> UpdateCategory(Guid id, UpdateCategoryRequest request, ICategoryCommandService service, HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var category = await service.UpdateAsync(id, request.Name, request.Slug, request.Description, correlationId, cancellationToken);
            return Results.Ok(category.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> DeleteCategory(Guid id, ICategoryCommandService service, HttpContext httpContext, CancellationToken cancellationToken)
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
