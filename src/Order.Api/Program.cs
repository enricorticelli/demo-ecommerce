using FluentValidation;
using Marten;
using Order.Api.Application;
using Order.Api.Endpoints;
using Shared.BuildingBlocks.Http;
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
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

builder.Services.AddHttpClient("cart", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("CART_API_URL") ?? "http://cart-api:8080");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("warehouse", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("WAREHOUSE_API_URL") ?? "http://warehouse-api:8080");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("payment", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("PAYMENT_API_URL") ?? "http://payment-api:8080");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("shipping", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("SHIPPING_API_URL") ?? "http://shipping-api:8080");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var connectionString = BuildConnectionString("ecommercedb");
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
    options.DatabaseSchemaName = "ordering";
});

builder.Host.UseWolverine(options =>
{
    options.UseRabbitMq(BuildRabbitMqConnection());
    options.Policies.AutoApplyTransactions();
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("default");
app.UseCorrelationId();


app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapOrderEndpoints();

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
