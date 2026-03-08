using Microsoft.Extensions.Configuration;

namespace Shipping.Infrastructure.Configuration;

public sealed record ShippingTechnicalOptions(
    string ShippingConnectionString,
    Uri RabbitMqUri,
    bool SkipWolverineBootstrap)
{
    public static ShippingTechnicalOptions FromConfiguration(IConfiguration configuration)
    {
        var connectionString = ResolveShippingConnectionString(configuration);
        var rabbitHost = configuration["MessageBus__Host"] ?? "rabbitmq";
        var rabbitPort = configuration["MessageBus__Port"] ?? "5672";
        var rabbitUsername = configuration["MessageBus__Username"] ?? "app";
        var rabbitPassword = configuration["MessageBus__Password"] ?? "app";
        var rabbitUri = new Uri($"amqp://{rabbitUsername}:{rabbitPassword}@{rabbitHost}:{rabbitPort}");

        var skipBootstrap = configuration.GetValue<bool>("Shipping:SkipWolverineBootstrap")
            || configuration.GetValue<bool>("Shipping:SkipDatabaseBootstrap");

        return new ShippingTechnicalOptions(connectionString, rabbitUri, skipBootstrap);
    }

    public static string ResolveShippingConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings__ShippingDb"] ?? configuration.GetConnectionString("ShippingDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Shipping connection string is missing.");
        }

        return connectionString;
    }
}
