using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Infrastructure;
using Warehouse.Application;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Warehouse.Infrastructure.Composition;

public static class WarehouseInfrastructureExtensions
{
    public static WebApplicationBuilder AddWarehouseInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddMarten(options =>
        {
            options.Connection(InfrastructureConnectionFactory.BuildPostgresConnectionString(
                Environment.GetEnvironmentVariable("WAREHOUSE_DB") ?? "warehousedb"));
            options.DatabaseSchemaName = "warehouse";
        });

        builder.Host.UseWolverine(options =>
        {
            options.UseRabbitMq(InfrastructureConnectionFactory.BuildRabbitMqConnectionString());
            options.Discovery.IncludeType<OrderPlacedHandler>();
            options.Policies.AutoApplyTransactions();
        });

        builder.Services.AddScoped<IWarehouseService, WarehouseService>();
        return builder;
    }
}
