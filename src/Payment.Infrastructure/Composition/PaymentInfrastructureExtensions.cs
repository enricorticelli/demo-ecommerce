using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application;
using Shared.BuildingBlocks.Infrastructure;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Payment.Infrastructure.Composition;

public static class PaymentInfrastructureExtensions
{
    public static WebApplicationBuilder AddPaymentInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddMarten(options =>
        {
            options.Connection(InfrastructureConnectionFactory.BuildPostgresConnectionString(
                Environment.GetEnvironmentVariable("PAYMENT_DB") ?? "paymentdb"));
            options.DatabaseSchemaName = "payment";
        });

        builder.Host.UseWolverine(options =>
        {
            options.UseRabbitMq(InfrastructureConnectionFactory.BuildRabbitMqConnectionString());
            options.Discovery.IncludeType<PaymentAuthorizeRequestedHandler>();
            options.Policies.AutoApplyTransactions();
        });

        builder.Services.AddScoped<PaymentService>();
        builder.Services.AddScoped<IPaymentService>(sp => sp.GetRequiredService<PaymentService>());
        builder.Services.AddScoped<IPaymentSessionService>(sp => sp.GetRequiredService<PaymentService>());
        return builder;
    }
}
