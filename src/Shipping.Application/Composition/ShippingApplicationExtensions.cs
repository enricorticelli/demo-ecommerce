using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Cqrs;

namespace Shipping.Application.Composition;

public static class ShippingApplicationExtensions
{
    public static IServiceCollection AddShippingApplication(this IServiceCollection services)
    {
        services.AddCqrs(typeof(ShippingApplicationExtensions).Assembly);
        return services;
    }
}
