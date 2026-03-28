using Evoluzione.TracedServiceCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Application.Abstractions.Commands;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Abstractions.Idempotency;
using Warehouse.Application.Abstractions.Services;
using Warehouse.Application.Services;
using Warehouse.Infrastructure.Idempotency;
using Warehouse.Infrastructure.Messaging;
using Warehouse.Infrastructure.Persistence;
using Warehouse.Infrastructure.Persistence.Repositories;
using Shared.BuildingBlocks.Contracts.Messaging;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Infrastructure.Configuration;

public static class WarehouseInfrastructureExtensions
{
    public static IServiceCollection AddWarehouseInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = WarehouseTechnicalOptions.ResolveWarehouseConnectionString(configuration);

        services.AddDbContextWithWolverineIntegration<WarehouseDbContext>(options => options.UseNpgsql(connectionString));
        services.AddTracedScoped<IWarehouseEventDeduplicationStore, PersistentWarehouseEventDeduplicationStore>();
        services.AddTracedScoped<IWarehouseStockRepository, WarehouseStockRepository>();
        services.AddTracedScoped<IWarehouseReservationRepository, WarehouseReservationRepository>();
        services.AddTracedScoped<IWarehouseCommandService, WarehouseCommandService>();
        services.AddTracedScoped<IWarehouseQueryService, WarehouseQueryService>();
        services.AddTracedScoped<IStockReservationService, StockReservationService>();
        services.AddTracedScoped<IDomainEventPublisher, OutboxDomainEventPublisher>();

        return services;
    }
}
