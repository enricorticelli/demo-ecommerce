using Microsoft.Extensions.Configuration;

namespace Communication.Infrastructure.Configuration;

public sealed record CommunicationTechnicalOptions(
    string CommunicationConnectionString,
    Uri RabbitMqUri,
    bool SkipWolverineBootstrap,
    string SmtpHost,
    int SmtpPort,
    string FromAddress,
    string FromName)
{
    public static CommunicationTechnicalOptions FromConfiguration(IConfiguration configuration)
    {
        var connectionString = ResolveCommunicationConnectionString(configuration);
        var rabbitHost = configuration["MessageBus__Host"] ?? "rabbitmq";
        var rabbitPort = configuration["MessageBus__Port"] ?? "5672";
        var rabbitUsername = configuration["MessageBus__Username"] ?? "app";
        var rabbitPassword = configuration["MessageBus__Password"] ?? "app";
        var rabbitUri = new Uri($"amqp://{rabbitUsername}:{rabbitPassword}@{rabbitHost}:{rabbitPort}");

        var skipBootstrap = configuration.GetValue<bool>("Communication:SkipWolverineBootstrap")
            || configuration.GetValue<bool>("Communication:SkipDatabaseBootstrap");

        var smtpHost = configuration["Communication__Smtp__Host"] ?? "mailpit";
        var smtpPort = configuration.GetValue<int?>("Communication__Smtp__Port") ?? 1025;
        var fromAddress = configuration["Communication__Smtp__FromAddress"] ?? "no-reply@ecommerce.local";
        var fromName = configuration["Communication__Smtp__FromName"] ?? "E-commerce";

        return new CommunicationTechnicalOptions(connectionString, rabbitUri, skipBootstrap, smtpHost, smtpPort, fromAddress, fromName);
    }

    public static string ResolveCommunicationConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings__CommunicationDb"] ?? configuration.GetConnectionString("CommunicationDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Communication connection string is missing.");
        }

        return connectionString;
    }
}
