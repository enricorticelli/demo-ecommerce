using Catalog.Api.Contracts;
using Catalog.Api.Contracts.Requests;
using Catalog.Api.Contracts.Responses;
using Catalog.Api.Mappers;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Api;

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

}
