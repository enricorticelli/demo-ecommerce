using Microsoft.Extensions.Configuration;

namespace Cart.Infrastructure.Configuration;

public sealed record CartTechnicalOptions(
    string CartConnectionString,
    Uri RabbitMqUri,
    bool SkipWolverineBootstrap)
{
    public static CartTechnicalOptions FromConfiguration(IConfiguration configuration)
    {
        var connectionString = ResolveCartConnectionString(configuration);
        var rabbitHost = configuration["MessageBus__Host"] ?? "rabbitmq";
        var rabbitPort = configuration["MessageBus__Port"] ?? "5672";
        var rabbitUsername = configuration["MessageBus__Username"] ?? "app";
        var rabbitPassword = configuration["MessageBus__Password"] ?? "app";
        var rabbitUri = new Uri($"amqp://{rabbitUsername}:{rabbitPassword}@{rabbitHost}:{rabbitPort}");
        var skipBootstrap = configuration.GetValue<bool>("Cart:SkipWolverineBootstrap");

        return new CartTechnicalOptions(connectionString, rabbitUri, skipBootstrap);
    }

    public static string ResolveCartConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings__CartDb"] ?? configuration.GetConnectionString("CartDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Cart connection string is missing.");
        }

        return connectionString;
    }
}
