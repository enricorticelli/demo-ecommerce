using Microsoft.AspNetCore.Builder;
using Order.Application.Handlers;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Warehouse;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace Order.Infrastructure.Configuration;

public static class OrderHostBuilderExtensions
{
    public static WebApplicationBuilder AddOrderModule(this WebApplicationBuilder builder)
    {
        var options = OrderTechnicalOptions.FromConfiguration(builder.Configuration);

        builder.Services.AddOrderInfrastructure(builder.Configuration);

        if (!options.SkipWolverineBootstrap)
        {
            builder.Host.UseWolverine(wolverine =>
            {
                wolverine.Discovery.IncludeAssembly(typeof(HandlePaymentAuthorizedOnOrderHandler).Assembly);
                wolverine.UseRabbitMq(options.RabbitMqUri).AutoProvision();

                // Outgoing integration events from Order
                wolverine.PublishMessage<OrderCreatedV1>().ToRabbitExchange("order-created");
                wolverine.PublishMessage<OrderCompletedV1>().ToRabbitExchange("order-completed");

                // Incoming integration events to Order
                wolverine.ListenToRabbitQueue("payment-authorized-order");
                wolverine.ListenToRabbitQueue("payment-rejected-order");
                wolverine.ListenToRabbitQueue("stock-reserved-order");
                wolverine.ListenToRabbitQueue("stock-rejected-order");

                wolverine.PersistMessagesWithPostgresql(options.OrderConnectionString);
                wolverine.UseEntityFrameworkCoreTransactions();
            });
        }

        return builder;
    }
}
