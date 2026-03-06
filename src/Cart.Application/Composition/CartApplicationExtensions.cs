using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Cqrs;

namespace Cart.Application.Composition;

public static class CartApplicationExtensions
{
    public static IServiceCollection AddCartApplication(this IServiceCollection services)
    {
        return services.AddModuleApplication(typeof(CartApplicationExtensions).Assembly);
    }
}
