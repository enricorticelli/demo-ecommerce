using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.BuildingBlocks.Cqrs;

public static class ModuleApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddModuleApplication(this IServiceCollection services, Assembly applicationAssembly)
    {
        services.AddCqrs(applicationAssembly);
        return services;
    }
}
