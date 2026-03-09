using Communication.Application.Abstractions.Email;
using Communication.Application.Abstractions.Idempotency;
using Communication.Infrastructure.Idempotency;
using Communication.Infrastructure.Persistence;
using Communication.Infrastructure.Services;
using Evoluzione.TracedServiceCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.EntityFrameworkCore;

namespace Communication.Infrastructure.Configuration;

public static class CommunicationInfrastructureExtensions
{
    public static IServiceCollection AddCommunicationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var options = CommunicationTechnicalOptions.FromConfiguration(configuration);

        services.AddDbContextWithWolverineIntegration<CommunicationDbContext>(db =>
            db.UseNpgsql(
                options.CommunicationConnectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(CommunicationDbContext).Assembly.FullName)));
        services.AddTracedScoped<ICommunicationEventDeduplicationStore, PersistentCommunicationEventDeduplicationStore>();
        services.AddTracedScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
