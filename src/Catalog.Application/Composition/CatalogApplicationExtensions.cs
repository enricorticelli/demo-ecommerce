using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application.Composition;

public static class CatalogApplicationExtensions
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddCqrs(typeof(CatalogApplicationExtensions).Assembly);
        return services;
    }
}
