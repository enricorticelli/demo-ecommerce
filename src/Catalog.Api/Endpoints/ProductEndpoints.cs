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

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var storeGroup = app.MapGroup(CatalogRoutes.StoreProducts)
            .WithTags("Catalog");

        storeGroup.MapGet("/", GetProducts).WithName("StoreGetProducts");
        storeGroup.MapGet("/{id:guid}", GetProductById).WithName("StoreGetProductById");
        storeGroup.MapGet("/new-arrivals", GetNewArrivals).WithName("StoreGetNewArrivals");
        storeGroup.MapGet("/best-sellers", GetBestSellers).WithName("StoreGetBestSellers");
        
        var backofficeGroup = app.MapGroup(CatalogRoutes.BackofficeProducts)
            .WithTags("Catalog");

        backofficeGroup.MapGet("/", GetProducts)
            .WithName("BackofficeGetProducts")
            .Produces<IEnumerable<ProductResponse>>();
        backofficeGroup.MapGet("/{id:guid}", GetProductById)
            .WithName("BackofficeGetProductById")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        backofficeGroup.MapPost("/", CreateProduct)
            .WithName("BackofficeCreateProduct")
            .Accepts<CreateProductRequest>("application/json")
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        backofficeGroup.MapPut("/{id:guid}", UpdateProduct)
            .WithName("BackofficeUpdateProduct")
            .Accepts<UpdateProductRequest>("application/json")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        backofficeGroup.MapDelete("/{id:guid}", DeleteProduct)
            .WithName("BackofficeDeleteProduct")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return storeGroup;
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

            return Results.Created($"{CatalogRoutes.BackofficeProducts}/{product.Id}", product.ToResponse());
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
