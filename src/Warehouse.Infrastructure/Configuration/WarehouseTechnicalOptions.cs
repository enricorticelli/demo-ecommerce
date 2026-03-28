using Microsoft.Extensions.Configuration;

namespace Warehouse.Infrastructure.Configuration;

public sealed record WarehouseTechnicalOptions(
    string WarehouseConnectionString,
    Uri RabbitMqUri,
    bool SkipWolverineBootstrap)
{
    public static WarehouseTechnicalOptions FromConfiguration(IConfiguration configuration)
    {
        var connectionString = ResolveWarehouseConnectionString(configuration);
        var rabbitHost = configuration["MessageBus__Host"] ?? "rabbitmq";
        var rabbitPort = configuration["MessageBus__Port"] ?? "5672";
        var rabbitUsername = configuration["MessageBus__Username"] ?? "app";
        var rabbitPassword = configuration["MessageBus__Password"] ?? "app";
        var rabbitUri = new Uri($"amqp://{rabbitUsername}:{rabbitPassword}@{rabbitHost}:{rabbitPort}");

        var skipBootstrap = configuration.GetValue<bool>("Warehouse:SkipWolverineBootstrap")
            || configuration.GetValue<bool>("Warehouse:SkipDatabaseBootstrap");

        return new WarehouseTechnicalOptions(connectionString, rabbitUri, skipBootstrap);
    }

    public static string ResolveWarehouseConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings__WarehouseDb"] ?? configuration.GetConnectionString("WarehouseDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Warehouse connection string is missing.");
        }

        return connectionString;
    }
}
