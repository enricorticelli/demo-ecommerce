using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Cqrs;

namespace Warehouse.Application.Composition;

public static class WarehouseApplicationExtensions
{
    public static IServiceCollection AddWarehouseApplication(this IServiceCollection services)
    {
        return services.AddModuleApplication(typeof(WarehouseApplicationExtensions).Assembly);
    }
}
