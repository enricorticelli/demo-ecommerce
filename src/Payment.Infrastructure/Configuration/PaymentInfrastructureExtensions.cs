using Evoluzione.TracedServiceCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Abstractions.Commands;
using Payment.Application.Abstractions.Idempotency;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Abstractions.Queries;
using Payment.Application.Abstractions.Repositories;
using Payment.Application.Abstractions.Services;
using Payment.Application.Services;
using Payment.Infrastructure.Idempotency;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Persistence.Repositories;
using Payment.Infrastructure.Providers;
using Shared.BuildingBlocks.Contracts.Messaging;
using Wolverine.EntityFrameworkCore;

namespace Payment.Infrastructure.Configuration;

public static class PaymentInfrastructureExtensions
{
    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = PaymentTechnicalOptions.ResolvePaymentConnectionString(configuration);

        services.AddDbContextWithWolverineIntegration<PaymentDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHttpClient(nameof(MockPaymentProviderAdapterBase));
        services.AddTracedScoped<IPaymentEventDeduplicationStore, PersistentPaymentEventDeduplicationStore>();
        services.AddTracedScoped<IPaymentWebhookDeduplicationStore, PersistentPaymentWebhookDeduplicationStore>();
        services.AddTracedScoped<IPaymentSessionRepository, PaymentSessionRepository>();
        services.AddTracedScoped<IPaymentSessionService, PaymentSessionService>();
        services.AddTracedScoped<IPaymentCheckoutService, PaymentCheckoutService>();
        services.AddTracedScoped<IPaymentWebhookService, PaymentWebhookService>();
        services.AddTracedScoped<IPaymentCommandService, PaymentCommandService>();
        services.AddTracedScoped<IPaymentQueryService, PaymentQueryService>();
        services.AddTracedScoped<IDomainEventPublisher, OutboxDomainEventPublisher>();

        services.AddScoped<StripeMockPaymentProviderAdapter>();
        services.AddScoped<PayPalMockPaymentProviderAdapter>();
        services.AddScoped<SatispayMockPaymentProviderAdapter>();

        services.AddScoped<IPaymentProviderClient>(sp => sp.GetRequiredService<StripeMockPaymentProviderAdapter>());
        services.AddScoped<IPaymentProviderClient>(sp => sp.GetRequiredService<PayPalMockPaymentProviderAdapter>());
        services.AddScoped<IPaymentProviderClient>(sp => sp.GetRequiredService<SatispayMockPaymentProviderAdapter>());

        services.AddScoped<IPaymentWebhookVerifier>(sp => sp.GetRequiredService<StripeMockPaymentProviderAdapter>());
        services.AddScoped<IPaymentWebhookVerifier>(sp => sp.GetRequiredService<PayPalMockPaymentProviderAdapter>());
        services.AddScoped<IPaymentWebhookVerifier>(sp => sp.GetRequiredService<SatispayMockPaymentProviderAdapter>());

        services.AddTracedScoped<IPaymentProviderRouter, PaymentProviderRouter>();

        return services;
    }
}
