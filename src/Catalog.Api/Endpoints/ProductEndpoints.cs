using Catalog.Api.Contracts;
using Catalog.Api.Contracts.Requests;
using Catalog.Api.Contracts.Responses;
using Catalog.Api.Mappers;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Api;

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

}
