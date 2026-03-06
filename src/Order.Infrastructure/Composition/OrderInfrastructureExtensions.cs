using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Order.Application;
using Order.Infrastructure.Clients;
using Order.Infrastructure.Messaging;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.WorkflowHandlers;
using Shared.BuildingBlocks.Infrastructure;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Order.Infrastructure.Composition;

public static class OrderInfrastructureExtensions
{
    public static WebApplicationBuilder AddOrderInfrastructure(this WebApplicationBuilder builder)
    {
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

        builder.Services.AddMarten(options =>
        {
            options.Connection(InfrastructureConnectionFactory.BuildPostgresConnectionString(
                Environment.GetEnvironmentVariable("ORDER_DB") ?? "orderdb"));
            options.DatabaseSchemaName = "ordering";
        });

        builder.Host.UseWolverine(options =>
        {
            options.UseRabbitMq(InfrastructureConnectionFactory.BuildRabbitMqConnectionString());
            options.Discovery.IncludeType<OrderStockReservedHandler>();
            options.Policies.AutoApplyTransactions();
        });

        builder.Services.AddScoped<ICartSnapshotClient, HttpCartSnapshotClient>();
        builder.Services.AddScoped<IWarehouseClient, HttpWarehouseClient>();
        builder.Services.AddScoped<IPaymentClient, HttpPaymentClient>();
        builder.Services.AddScoped<IShippingClient, HttpShippingClient>();
        builder.Services.AddScoped<IOrderStateStore, MartenOrderStateStore>();
        builder.Services.AddScoped<IOrderEventPublisher, WolverineOrderEventPublisher>();
        builder.Services.AddScoped<IOrderWorkflowProcessor, OrderWorkflowProcessor>();
        builder.Services.AddScoped<IOrderService, OrderService>();

        return builder;
    }
}
