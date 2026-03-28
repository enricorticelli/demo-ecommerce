using Microsoft.AspNetCore.Builder;
using Payment.Application.Handlers;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace Payment.Infrastructure.Configuration;

public static class PaymentHostBuilderExtensions
{
    public static WebApplicationBuilder AddPaymentModule(this WebApplicationBuilder builder)
    {
        var options = PaymentTechnicalOptions.FromConfiguration(builder.Configuration);

        builder.Services.AddPaymentInfrastructure(builder.Configuration);

        if (!options.SkipWolverineBootstrap)
        {
            builder.Host.UseWolverine(wolverine =>
            {
                wolverine.Discovery.IncludeAssembly(typeof(AuthorizePaymentOnOrderCreatedHandler).Assembly);
                var rabbitMq = wolverine.UseRabbitMq(options.RabbitMqUri);
                rabbitMq.AutoProvision();
                rabbitMq.BindExchange("order-created", ExchangeType.Fanout).ToQueue("order-created-payment");

                // Incoming integration events to Payment
                wolverine.ListenToRabbitQueue("order-created-payment");

                // Outgoing integration events from Payment
                wolverine.PublishMessage<PaymentAuthorizedV1>().ToRabbitQueue("payment-authorized-order");
                wolverine.PublishMessage<PaymentRejectedV1>().ToRabbitQueue("payment-rejected-order");

                wolverine.PersistMessagesWithPostgresql(options.PaymentConnectionString);
                wolverine.UseEntityFrameworkCoreTransactions();
            });
        }

        return builder;
    }
}
