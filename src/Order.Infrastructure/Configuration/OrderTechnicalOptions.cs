using Microsoft.Extensions.Configuration;

namespace Order.Infrastructure.Configuration;

public sealed record OrderTechnicalOptions(
    string OrderConnectionString,
    Uri RabbitMqUri,
    bool SkipWolverineBootstrap)
{
    public static OrderTechnicalOptions FromConfiguration(IConfiguration configuration)
    {
        var connectionString = ResolveOrderConnectionString(configuration);
        var rabbitHost = configuration["MessageBus__Host"] ?? "rabbitmq";
        var rabbitPort = configuration["MessageBus__Port"] ?? "5672";
        var rabbitUsername = configuration["MessageBus__Username"] ?? "app";
        var rabbitPassword = configuration["MessageBus__Password"] ?? "app";
        var rabbitUri = new Uri($"amqp://{rabbitUsername}:{rabbitPassword}@{rabbitHost}:{rabbitPort}");
        var skipBootstrap = configuration.GetValue<bool>("Order:SkipWolverineBootstrap");

        return new OrderTechnicalOptions(connectionString, rabbitUri, skipBootstrap);
    }

    public static string ResolveOrderConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings__OrderDb"] ?? configuration.GetConnectionString("OrderDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Order connection string is missing.");
        }

        return connectionString;
    }
}
