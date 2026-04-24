using Catalog.Api.Contracts;
using Catalog.Api.Contracts.Requests;
using Catalog.Api.Contracts.Responses;
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

        backofficeGroup.MapGet("/", GetBrands)
            .WithName("BackofficeGetBrands")
            .Produces<IEnumerable<BrandResponse>>();
        backofficeGroup.MapGet("/{id:guid}", GetBrandById)
            .WithName("BackofficeGetBrandById")
            .Produces<BrandResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        backofficeGroup.MapPost("/", CreateBrand)
            .WithName("BackofficeCreateBrand")
            .Accepts<CreateBrandRequest>("application/json")
            .Produces<BrandResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        backofficeGroup.MapPut("/{id:guid}", UpdateBrand)
            .WithName("BackofficeUpdateBrand")
            .Accepts<UpdateBrandRequest>("application/json")
            .Produces<BrandResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        backofficeGroup.MapDelete("/{id:guid}", DeleteBrand)
            .WithName("BackofficeDeleteBrand")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

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
