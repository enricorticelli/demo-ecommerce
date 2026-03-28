using Microsoft.AspNetCore.Builder;
using Warehouse.Application.Handlers;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Warehouse;
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
                var rabbitMq = wolverine.UseRabbitMq(options.RabbitMqUri);
                rabbitMq.AutoProvision();
                rabbitMq.BindExchange("order-created", ExchangeType.Fanout).ToQueue("order-created-warehouse");

                // Incoming integration events to Warehouse
                wolverine.ListenToRabbitQueue("order-created-warehouse");

                // Outgoing integration events from Warehouse
                wolverine.PublishMessage<StockReservedV1>().ToRabbitQueue("stock-reserved-order");
                wolverine.PublishMessage<StockRejectedV1>().ToRabbitQueue("stock-rejected-order");

                wolverine.PersistMessagesWithPostgresql(options.WarehouseConnectionString);
                wolverine.UseEntityFrameworkCoreTransactions();
            });
        }

        return builder;
    }
}
