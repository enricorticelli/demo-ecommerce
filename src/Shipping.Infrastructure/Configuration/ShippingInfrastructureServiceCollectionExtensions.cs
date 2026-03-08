using Evoluzione.TracedServiceCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Mapping;
using Shipping.Application.Abstractions.Idempotency;
using Shipping.Application.Abstractions.Commands;
using Shipping.Application.Abstractions.Queries;
using Shipping.Application.Abstractions.Repositories;
using Shipping.Infrastructure.Idempotency;
using Shipping.Application.Mappers;
using Shipping.Application.Services;
using Shipping.Application.Views;
using Shipping.Infrastructure.Persistence;
using Shipping.Infrastructure.Persistence.Repositories;
using Wolverine.EntityFrameworkCore;

namespace Shipping.Infrastructure.Configuration;

public static class ShippingInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddShippingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ShippingTechnicalOptions.ResolveShippingConnectionString(configuration);

        services.AddDbContextWithWolverineIntegration<ShippingDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(ShippingDbContext).Assembly.FullName)));
        services.AddTracedScoped<IShipmentRepository, ShipmentRepository>();
        services.AddTracedScoped<IShippingEventDeduplicationStore, PersistentShippingEventDeduplicationStore>();
        services.AddScoped<IViewMapper<Domain.Entities.Shipment, ShipmentView>, ShipmentViewMapper>();
        services.AddTracedScoped<IShippingCommandService, ShippingCommandService>();
        services.AddTracedScoped<IShippingQueryService, ShippingQueryService>();

        return services;
    }
}
