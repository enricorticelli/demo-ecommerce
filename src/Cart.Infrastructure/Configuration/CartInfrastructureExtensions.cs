using Cart.Application.Abstractions.Idempotency;
using Cart.Application.Abstractions.Commands;
using Cart.Application.Abstractions.Queries;
using Cart.Application.Abstractions.Repositories;
using Cart.Application.Mappers;
using Cart.Application.Services;
using Cart.Application.Views;
using Cart.Infrastructure.Persistence;
using Cart.Infrastructure.Idempotency;
using Cart.Infrastructure.Persistence.Repositories;
using Evoluzione.TracedServiceCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Mapping;
using Wolverine.EntityFrameworkCore;

namespace Cart.Infrastructure.Configuration;

public static class CartInfrastructureExtensions
{
    public static IServiceCollection AddCartInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = CartTechnicalOptions.ResolveCartConnectionString(configuration);

        services.AddDbContextWithWolverineIntegration<CartDbContext>(options => options.UseNpgsql(connectionString));
        services.AddTracedScoped<ICartRepository, CartRepository>();
        services.AddTracedScoped<ICartEventDeduplicationStore, InMemoryCartEventDeduplicationStore>();
        services.AddScoped<IViewMapper<Cart.Domain.Entities.Cart, CartView>, CartViewMapper>();
        services.AddTracedScoped<ICartCommandService, CartCommandService>();
        services.AddTracedScoped<ICartQueryService, CartQueryService>();

        return services;
    }
}
