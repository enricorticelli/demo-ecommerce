using Catalog.Api.Contracts;
using Catalog.Api.Contracts.Requests;
using Catalog.Api.Mappers;
using Catalog.Application.Abstractions.Commands;
using Catalog.Application.Abstractions.Queries;
using Shared.BuildingBlocks.Api.Correlation;
using Shared.BuildingBlocks.Api.Errors;
using Shared.BuildingBlocks.Api.Pagination;

namespace Catalog.Api.Endpoints;

public static class BrandEndpoints
{
    public static RouteGroupBuilder MapBrandEndpoints(this IEndpointRouteBuilder app)
    {
        var backofficeGroup = app.MapGroup(CatalogRoutes.BackofficeBrands)
            .WithTags("Catalog");

        backofficeGroup.MapGet("/", GetBrands).WithName("BackofficeGetBrands");
        backofficeGroup.MapGet("/{id:guid}", GetBrandById).WithName("BackofficeGetBrandById");
        backofficeGroup.MapPost("/", CreateBrand).WithName("BackofficeCreateBrand");
        backofficeGroup.MapPut("/{id:guid}", UpdateBrand).WithName("BackofficeUpdateBrand");
        backofficeGroup.MapDelete("/{id:guid}", DeleteBrand).WithName("BackofficeDeleteBrand");

        return backofficeGroup;
    }

    private static async Task<IResult> GetBrands(
        int? limit,
        int? offset,
        string? searchTerm,
        IBrandQueryService service,
        CancellationToken cancellationToken)
    {
        var (normalizedLimit, normalizedOffset) = PaginationNormalizer.Normalize(limit, offset);
        var brands = await service.ListAsync(searchTerm, cancellationToken);
        return Results.Ok(brands
            .Skip(normalizedOffset)
            .Take(normalizedLimit)
            .Select(x => x.ToResponse()));
    }

    private static async Task<IResult> GetBrandById(Guid id, IBrandQueryService service, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await service.GetByIdAsync(id, cancellationToken);
            return Results.Ok(brand.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> CreateBrand(CreateBrandRequest request, IBrandCommandService service, HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var brand = await service.CreateAsync(request.Name, request.Slug, request.Description, correlationId, cancellationToken);
            var response = brand.ToResponse();
            return Results.Created($"{CatalogRoutes.BackofficeBrands}/{brand.Id}", response);
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> UpdateBrand(Guid id, UpdateBrandRequest request, IBrandCommandService service, HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var brand = await service.UpdateAsync(id, request.Name, request.Slug, request.Description, correlationId, cancellationToken);
            return Results.Ok(brand.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> DeleteBrand(Guid id, IBrandCommandService service, HttpContext httpContext, CancellationToken cancellationToken)
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
