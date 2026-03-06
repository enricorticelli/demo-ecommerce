using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Cqrs;

namespace User.Application.Composition;

public static class UserApplicationExtensions
{
    public static IServiceCollection AddUserApplication(this IServiceCollection services)
    {
        services.AddCqrs(typeof(UserApplicationExtensions).Assembly);
        return services;
    }
}
