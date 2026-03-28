using Microsoft.Extensions.Configuration;

namespace Payment.Infrastructure.Configuration;

public sealed record PaymentTechnicalOptions(
    string PaymentConnectionString,
    Uri RabbitMqUri,
    bool SkipWolverineBootstrap)
{
    public static PaymentTechnicalOptions FromConfiguration(IConfiguration configuration)
    {
        var connectionString = ResolvePaymentConnectionString(configuration);
        var rabbitHost = configuration["MessageBus__Host"] ?? "rabbitmq";
        var rabbitPort = configuration["MessageBus__Port"] ?? "5672";
        var rabbitUsername = configuration["MessageBus__Username"] ?? "app";
        var rabbitPassword = configuration["MessageBus__Password"] ?? "app";
        var rabbitUri = new Uri($"amqp://{rabbitUsername}:{rabbitPassword}@{rabbitHost}:{rabbitPort}");

        var skipBootstrap = configuration.GetValue<bool>("Payment:SkipWolverineBootstrap")
            || configuration.GetValue<bool>("Payment:SkipDatabaseBootstrap");

        return new PaymentTechnicalOptions(connectionString, rabbitUri, skipBootstrap);
    }

    public static string ResolvePaymentConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings__PaymentDb"] ?? configuration.GetConnectionString("PaymentDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Payment connection string is missing.");
        }

        return connectionString;
    }
}
