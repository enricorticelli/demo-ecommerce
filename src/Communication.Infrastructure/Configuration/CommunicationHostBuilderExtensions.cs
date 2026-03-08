using Communication.Application.Handlers;
using Microsoft.AspNetCore.Builder;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace Communication.Infrastructure.Configuration;

public static class CommunicationHostBuilderExtensions
{
    public static WebApplicationBuilder AddCommunicationModule(this WebApplicationBuilder builder)
    {
        var options = CommunicationTechnicalOptions.FromConfiguration(builder.Configuration);

        builder.Services.AddCommunicationInfrastructure(builder.Configuration);

        if (!options.SkipWolverineBootstrap)
        {
            builder.Host.UseWolverine(wolverine =>
            {
                wolverine.Discovery.IncludeAssembly(typeof(SendOrderCompletedEmailHandler).Assembly);
                var rabbitMq = wolverine.UseRabbitMq(options.RabbitMqUri);
                rabbitMq.AutoProvision();
                rabbitMq.BindExchange("order-completed", ExchangeType.Fanout).ToQueue("order-completed-communication");

                wolverine.ListenToRabbitQueue("order-completed-communication");

                wolverine.PersistMessagesWithPostgresql(options.CommunicationConnectionString);
                wolverine.UseEntityFrameworkCoreTransactions();
            });
        }

        return builder;
    }
}
