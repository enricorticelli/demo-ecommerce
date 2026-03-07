using Catalog.Api.Contracts;
using Catalog.Api.Contracts.Requests;
using Catalog.Api.Contracts.Responses;
using Catalog.Api.Mappers;
using Catalog.Application.Abstractions.Commands;
using Catalog.Application.Abstractions.Queries;
using Shared.BuildingBlocks.Api.Correlation;
using Shared.BuildingBlocks.Api.Errors;

namespace Catalog.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(CatalogRoutes.Products)
            .WithTags("Catalog");

        group.MapGet("/", GetProducts).WithName("GetProducts");
        group.MapGet("/new-arrivals", GetNewArrivals).WithName("GetNewArrivals");
        group.MapGet("/best-sellers", GetBestSellers).WithName("GetBestSellers");
        group.MapGet("/{id:guid}", GetProductById).WithName("GetProductById");
        group.MapPost("/", CreateProduct).WithName("CreateProduct");
        group.MapPut("/{id:guid}", UpdateProduct).WithName("UpdateProduct");
        group.MapDelete("/{id:guid}", DeleteProduct).WithName("DeleteProduct");

        return group;
    }

    private static async Task<IResult> GetProducts(string? searchTerm, IProductQueryService service, CancellationToken cancellationToken)
    {
        var products = await service.ListAsync(searchTerm, cancellationToken);
        return Results.Ok(products.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> GetNewArrivals(string? searchTerm, IProductQueryService service, CancellationToken cancellationToken)
    {
        var products = await service.ListNewArrivalsAsync(searchTerm, cancellationToken);
        return Results.Ok(products.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> GetBestSellers(string? searchTerm, IProductQueryService service, CancellationToken cancellationToken)
    {
        var products = await service.ListBestSellersAsync(searchTerm, cancellationToken);
        return Results.Ok(products.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> GetProductById(Guid id, IProductQueryService service, CancellationToken cancellationToken)
    {
        try
        {
            var product = await service.GetByIdAsync(id, cancellationToken);
            return Results.Ok(product.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> CreateProduct(CreateProductRequest request, IProductCommandCatalogService service, HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var product = await service.CreateAsync(
            request.Sku,
            request.Name,
            request.Description,
            request.Price,
            request.BrandId,
            request.CategoryId,
            request.CollectionIds,
            request.IsNewArrival,
            request.IsBestSeller,
            correlationId,
            cancellationToken);

            return Results.Created($"{CatalogRoutes.Products}/{product.Id}", product.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> UpdateProduct(Guid id, UpdateProductRequest request, IProductCommandCatalogService service, HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var product = await service.UpdateAsync(
            id,
            request.Sku,
            request.Name,
            request.Description,
            request.Price,
            request.BrandId,
            request.CategoryId,
            request.CollectionIds,
            request.IsNewArrival,
            request.IsBestSeller,
            correlationId,
            cancellationToken);

            return Results.Ok(product.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> DeleteProduct(Guid id, IProductCommandCatalogService service, HttpContext httpContext, CancellationToken cancellationToken)
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
