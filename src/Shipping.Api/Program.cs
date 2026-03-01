using Marten;
using Shared.BuildingBlocks.Http;
using Shipping.Api.Endpoints;
using Shipping.Api.Handlers;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var connectionString = BuildConnectionString("ecommercedb");
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
    options.DatabaseSchemaName = "shipping";
});

builder.Host.UseWolverine(options =>
{
    options.UseRabbitMq(BuildRabbitMqConnection());
    options.Discovery.IncludeType<ShippingHandlers>();
    options.Policies.AutoApplyTransactions();
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("default");
app.UseCorrelationId();


app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapShippingEndpoints();

await app.RunAsync();
return;

static string BuildConnectionString(string database)
{
    var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "postgres";
    var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
    var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
    var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
    return $"Host={host};Port={port};Database={database};Username={user};Password={password}";
}

static string BuildRabbitMqConnection()
{
    var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
    var port = Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672";
    var user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
    var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
    return $"amqp://{user}:{password}@{host}:{port}";
}
