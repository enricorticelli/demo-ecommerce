using Cart.Application;
using Cart.Infrastructure.Persistence.ReadModels;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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
            options.UseRabbitMq(InfrastructureConnectionFactory.BuildRabbitMqConnectionString());
            options.Policies.AutoApplyTransactions();
        });

        builder.Services.AddScoped<ICartReadModelStore, MongoCartReadModelStore>();
        builder.Services.AddScoped<ICartService, CartService>();
        return builder;
    }
}
