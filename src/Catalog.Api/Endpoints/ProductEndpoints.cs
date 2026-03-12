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

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var storeGroup = app.MapGroup(CatalogRoutes.StoreProducts)
            .WithTags("Catalog");

        storeGroup.MapGet("/", GetProducts).WithName("StoreGetProducts");
        storeGroup.MapGet("/new-arrivals", GetNewArrivals).WithName("StoreGetNewArrivals");
        storeGroup.MapGet("/best-sellers", GetBestSellers).WithName("StoreGetBestSellers");
        storeGroup.MapGet("/{id:guid}", GetProductById).WithName("StoreGetProductById");

        var adminGroup = app.MapGroup(CatalogRoutes.AdminProducts)
            .WithTags("Catalog");

        adminGroup.MapGet("/", AdminGetProducts).RequireAuthorization(AuthorizationPolicies.CatalogReadPolicy).WithName("AdminGetProducts");
        adminGroup.MapGet("/{id:guid}", GetProductById).RequireAuthorization(AuthorizationPolicies.CatalogReadPolicy).WithName("AdminGetProductById");
        adminGroup.MapPost("/", CreateProduct).RequireAuthorization(AuthorizationPolicies.CatalogWritePolicy).WithName("AdminCreateProduct");
        adminGroup.MapPut("/{id:guid}", UpdateProduct).RequireAuthorization(AuthorizationPolicies.CatalogWritePolicy).WithName("AdminUpdateProduct");
        adminGroup.MapDelete("/{id:guid}", DeleteProduct).RequireAuthorization(AuthorizationPolicies.CatalogWritePolicy).WithName("AdminDeleteProduct");

        return adminGroup;
    }

    private static async Task<IResult> GetProducts(string? searchTerm, IProductQueryService service, CancellationToken cancellationToken)
    {
        var products = await service.ListAsync(searchTerm, cancellationToken);
        return Results.Ok(products.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> AdminGetProducts(
        int? limit,
        int? offset,
        string? searchTerm,
        IProductQueryService service,
        CancellationToken cancellationToken)
    {
        var (normalizedLimit, normalizedOffset) = PaginationNormalizer.Normalize(limit, offset);
        var products = await service.ListAsync(searchTerm, cancellationToken);
        return Results.Ok(products
            .Skip(normalizedOffset)
            .Take(normalizedLimit)
            .Select(x => x.ToResponse()));
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

            return Results.Created($"{CatalogRoutes.AdminProducts}/{product.Id}", product.ToResponse());
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
