using Microsoft.AspNetCore.Builder;
using Warehouse.Application.Handlers;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace Warehouse.Infrastructure.Configuration;

public static class WarehouseHostBuilderExtensions
{
    public static WebApplicationBuilder AddWarehouseModule(this WebApplicationBuilder builder)
    {
        var options = WarehouseTechnicalOptions.FromConfiguration(builder.Configuration);

        builder.Services.AddWarehouseInfrastructure(builder.Configuration);

        if (!options.SkipWolverineBootstrap)
        {
            builder.Host.UseWolverine(wolverine =>
            {
                wolverine.Discovery.IncludeAssembly(typeof(ReserveStockOnOrderCreatedHandler).Assembly);
                wolverine.UseRabbitMq(options.RabbitMqUri).AutoProvision();
                wolverine.PersistMessagesWithPostgresql(options.WarehouseConnectionString);
                wolverine.UseEntityFrameworkCoreTransactions();
            });
        }

        return builder;
    }
}
