using Marten;
using Microsoft.AspNetCore.Http.HttpResults;
using User.Api.Domain;

namespace User.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/users")
            .WithTags("User")
            ;

        group.MapGet("/{id:guid}", GetUser)
            .WithName("GetUserById");

        var internalGroup = app.MapGroup("/internal/seed")
            .WithTags("UserInternal")
            ;

        internalGroup.MapPost("/users", SeedUsers)
            .WithName("SeedUsers");

        return group;
    }

    private static async Task<Results<Ok<UserDocument>, NotFound>> GetUser(Guid id, IQuerySession session, CancellationToken cancellationToken)
    {
        var user = await session.LoadAsync<UserDocument>(id, cancellationToken);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(user);
    }

    private static async Task<Ok<object>> SeedUsers(IDocumentSession session, CancellationToken cancellationToken)
    {
        var users = new[]
        {
            new UserDocument { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Email = "demo@cqrs-ecommerce.local", FullName = "Demo Customer" }
        };

        foreach (var user in users)
        {
            session.Store(user);
        }

        await session.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((object)new { Seeded = users.Length });
    }
}
