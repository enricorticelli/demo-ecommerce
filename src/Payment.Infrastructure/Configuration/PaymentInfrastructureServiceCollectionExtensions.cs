using Evoluzione.TracedServiceCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Abstractions.Idempotency;
using Payment.Application.Abstractions.Repositories;
using Payment.Application.Abstractions.Services;
using Payment.Application.Services;
using Payment.Infrastructure.Idempotency;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Persistence.Repositories;
using Payment.Infrastructure.Services;
using Shared.BuildingBlocks.Contracts.Messaging;
using Wolverine.EntityFrameworkCore;

namespace Payment.Infrastructure.Configuration;

public static class PaymentInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = PaymentTechnicalOptions.ResolvePaymentConnectionString(configuration);

        services.AddDbContextWithWolverineIntegration<PaymentDbContext>(options => options.UseNpgsql(connectionString));
        services.AddTracedScoped<IPaymentEventDeduplicationStore, PersistentPaymentEventDeduplicationStore>();
        services.AddTracedScoped<IPaymentSessionRepository, PaymentSessionRepository>();
        services.AddTracedScoped<IPaymentAuthorizationService, InMemoryPaymentAuthorizationService>();
        services.AddTracedScoped<IPaymentSessionService, PaymentSessionService>();
        services.AddTracedScoped<IDomainEventPublisher, OutboxDomainEventPublisher>();

        return services;
    }
}
