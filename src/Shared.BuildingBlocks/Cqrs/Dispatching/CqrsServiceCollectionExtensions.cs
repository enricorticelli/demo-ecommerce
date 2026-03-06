using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shared.BuildingBlocks.Cqrs;

public static class CqrsServiceCollectionExtensions
{
    public static IServiceCollection AddCqrs(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddScoped<ICommandDispatcher, CqrsDispatcher>();
        services.AddScoped<IQueryDispatcher, CqrsDispatcher>();

        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>)));

        var targets = assemblies.Where(a => !a.IsDynamic).Distinct().ToArray();
        foreach (var type in targets.SelectMany(a => a.DefinedTypes).Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var serviceType in type.ImplementedInterfaces.Where(IsHandledInterface))
            {
                services.TryAddTransient(serviceType, type.AsType());
            }
        }

        return services;
    }

    private static bool IsHandledInterface(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(ICommandHandler<,>)
               || definition == typeof(IQueryHandler<,>)
               || definition == typeof(IRequestValidator<>);
    }
}
