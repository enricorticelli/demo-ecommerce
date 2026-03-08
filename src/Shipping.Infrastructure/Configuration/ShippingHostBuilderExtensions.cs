using Microsoft.AspNetCore.Builder;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace Shipping.Infrastructure.Configuration;

public static class ShippingHostBuilderExtensions
{
    public static WebApplicationBuilder AddShippingModule(this WebApplicationBuilder builder)
    {
        var options = ShippingTechnicalOptions.FromConfiguration(builder.Configuration);

        builder.Services.AddShippingInfrastructure(builder.Configuration);

        if (!options.SkipWolverineBootstrap)
        {
            builder.Host.UseWolverine(wolverine =>
            {
                wolverine.UseRabbitMq(options.RabbitMqUri).AutoProvision();
                wolverine.PersistMessagesWithPostgresql(options.ShippingConnectionString);
                wolverine.UseEntityFrameworkCoreTransactions();
            });
        }

        return builder;
    }
}
