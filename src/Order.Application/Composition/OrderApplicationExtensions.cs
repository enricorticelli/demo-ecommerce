using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Cqrs;

namespace Order.Application.Composition;

public static class OrderApplicationExtensions
{
    public static IServiceCollection AddOrderApplication(this IServiceCollection services)
    {
        return services.AddModuleApplication(typeof(OrderApplicationExtensions).Assembly);
    }
}
