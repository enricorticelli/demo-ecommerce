using Catalog.Api.Domain;
using Marten;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Catalog.Api.Endpoints;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/products")
            .WithTags("Catalog")
            ;

        group.MapGet("/", GetProducts)
            .WithName("GetProducts");

        group.MapGet("/{id:guid}", GetProductById)
            .WithName("GetProductById");

        var internalGroup = app.MapGroup("/internal/seed")
            .WithTags("CatalogInternal")
            ;

        internalGroup.MapPost("/products", SeedProducts)
            .WithName("SeedProducts");

        return group;
    }

    private static async Task<Ok<IReadOnlyList<ProductDocument>>> GetProducts(IQuerySession session, CancellationToken cancellationToken)
    {
        var products = await session.Query<ProductDocument>().OrderBy(p => p.Name).ToListAsync(cancellationToken);
        return TypedResults.Ok<IReadOnlyList<ProductDocument>>(products);
    }

    private static async Task<Results<Ok<ProductDocument>, NotFound>> GetProductById(Guid id, IQuerySession session, CancellationToken cancellationToken)
    {
        var product = await session.LoadAsync<ProductDocument>(id, cancellationToken);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    private static async Task<Ok<object>> SeedProducts(IDocumentSession session, CancellationToken cancellationToken)
    {
        var products = new[]
        {
            new ProductDocument { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Sku = "SKU-KEYBOARD-001", Name = "Mechanical Keyboard", Description = "Fast switch keyboard", Price = 89.99m },
            new ProductDocument { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Sku = "SKU-MOUSE-001", Name = "Gaming Mouse", Description = "Low latency mouse", Price = 49.90m },
            new ProductDocument { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Sku = "SKU-HEADSET-001", Name = "Wireless Headset", Description = "Surround audio headset", Price = 129.00m }
        };

        foreach (var product in products)
        {
            session.Store(product);
        }

        await session.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((object)new { Seeded = products.Length });
    }
}
