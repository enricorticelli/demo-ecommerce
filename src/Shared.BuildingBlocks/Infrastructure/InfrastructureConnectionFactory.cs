namespace Shared.BuildingBlocks.Infrastructure;

public static class InfrastructureConnectionFactory
{
    public static string BuildPostgresConnectionString(string database)
    {
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "postgres";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
        return $"Host={host};Port={port};Database={database};Username={user};Password={password}";
    }

    public static string BuildRabbitMqConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
        var port = Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672";
        var user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
        var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
        return $"amqp://{user}:{password}@{host}:{port}";
    }

    public static string BuildMongoConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("MONGO_HOST") ?? "mongodb";
        var port = Environment.GetEnvironmentVariable("MONGO_PORT") ?? "27017";
        var user = Environment.GetEnvironmentVariable("MONGO_USER");
        var password = Environment.GetEnvironmentVariable("MONGO_PASSWORD");

        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
        {
            return $"mongodb://{user}:{password}@{host}:{port}";
        }

        return $"mongodb://{host}:{port}";
    }
}
