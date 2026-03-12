using Catalog.Api.Contracts;
using Catalog.Api.Contracts.Requests;
using Catalog.Api.Mappers;
using Catalog.Application.Abstractions.Commands;
using Catalog.Application.Abstractions.Queries;
using Shared.BuildingBlocks.Api.Correlation;
using Shared.BuildingBlocks.Api.Errors;
using Shared.BuildingBlocks.Api.Pagination;
using Shared.BuildingBlocks.Api;

namespace Catalog.Api.Endpoints;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(CatalogRoutes.AdminCategories)
            .WithTags("Catalog");

        group.MapGet("/", GetCategories).RequireAuthorization(AuthorizationPolicies.CatalogReadPolicy).WithName("AdminGetCategories");
        group.MapGet("/{id:guid}", GetCategoryById).RequireAuthorization(AuthorizationPolicies.CatalogReadPolicy).WithName("AdminGetCategoryById");
        group.MapPost("/", CreateCategory).RequireAuthorization(AuthorizationPolicies.CatalogWritePolicy).WithName("AdminCreateCategory");
        group.MapPut("/{id:guid}", UpdateCategory).RequireAuthorization(AuthorizationPolicies.CatalogWritePolicy).WithName("AdminUpdateCategory");
        group.MapDelete("/{id:guid}", DeleteCategory).RequireAuthorization(AuthorizationPolicies.CatalogWritePolicy).WithName("AdminDeleteCategory");

        return group;
    }

    private static async Task<IResult> GetCategories(
        int? limit,
        int? offset,
        string? searchTerm,
        ICategoryQueryService service,
        CancellationToken cancellationToken)
    {
        var (normalizedLimit, normalizedOffset) = PaginationNormalizer.Normalize(limit, offset);
        var categories = await service.ListAsync(searchTerm, cancellationToken);
        return Results.Ok(categories
            .Skip(normalizedOffset)
            .Take(normalizedLimit)
            .Select(x => x.ToResponse()));
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
            return Results.Created($"{CatalogRoutes.AdminCategories}/{category.Id}", response);
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
