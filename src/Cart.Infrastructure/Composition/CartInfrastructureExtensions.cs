using Cart.Application;
using Cart.Application.Abstractions;
using Cart.Infrastructure.Messaging.Handlers;
using Cart.Infrastructure.Persistence.ReadModels;
using Cart.Infrastructure.Services;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Contracts.Integration;
using Shared.BuildingBlocks.Infrastructure;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Cart.Infrastructure.Composition;

public static class CartInfrastructureExtensions
{
    public static WebApplicationBuilder AddCartInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddMarten(options =>
        {
            options.Connection(InfrastructureConnectionFactory.BuildPostgresConnectionString(
                Environment.GetEnvironmentVariable("CART_DB") ?? "cartdb"));
            options.DatabaseSchemaName = "cart";
        });

        builder.Host.UseWolverine(options =>
        {
            options.UseRabbitMq(InfrastructureConnectionFactory.BuildRabbitMqConnectionString())
                .AutoProvision();
            options.ListenToRabbitQueue(IntegrationQueueNames.CartWorkflow);
            options.Discovery.IncludeType<OrderCompletedHandler>();
            options.Policies.AutoApplyTransactions();
        });

        builder.Services.AddScoped<ICartReadModelStore, MongoCartReadModelStore>();
        builder.Services.AddScoped<ICartService, CartService>();
        return builder;
    }
}
