using Microsoft.AspNetCore.Builder;
using Order.Application.Handlers;
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
                wolverine.PersistMessagesWithPostgresql(options.OrderConnectionString);
                wolverine.UseEntityFrameworkCoreTransactions();
            });
        }

        return builder;
    }
}
