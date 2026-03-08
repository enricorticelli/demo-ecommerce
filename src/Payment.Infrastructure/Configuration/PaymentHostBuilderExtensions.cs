using Microsoft.AspNetCore.Builder;
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
                wolverine.UseRabbitMq(options.RabbitMqUri).AutoProvision();
                wolverine.PersistMessagesWithPostgresql(options.PaymentConnectionString);
                wolverine.UseEntityFrameworkCoreTransactions();
            });
        }

        return builder;
    }
}
