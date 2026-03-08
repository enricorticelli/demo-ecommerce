using Evoluzione.TracedServiceCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Abstractions.Commands;
using Order.Application.Abstractions.Idempotency;
using Order.Application.Abstractions.Queries;
using Order.Application.Abstractions.Repositories;
using Order.Application.Abstractions.Rules;
using Order.Application.Mappers;
using Order.Application.Services;
using Order.Application.Services.Rules;
using Order.Application.Views;
using Order.Infrastructure.Idempotency;
using Order.Infrastructure.Messaging;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Persistence.Repositories;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Mapping;
using Wolverine.EntityFrameworkCore;

namespace Order.Infrastructure.Configuration;

public static class OrderInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddOrderInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = OrderTechnicalOptions.ResolveOrderConnectionString(configuration);

        services.AddDbContextWithWolverineIntegration<OrderDbContext>(options => options.UseNpgsql(connectionString));
        services.AddTracedScoped<IOrderRepository, OrderRepository>();
        services.AddTracedScoped<IOrderEventDeduplicationStore, PersistentOrderEventDeduplicationStore>();
        services.AddTracedScoped<IDomainEventPublisher, OutboxDomainEventPublisher>();
        services.AddTracedScoped<IOrderRules, OrderRules>();
        services.AddTracedScoped<IViewMapper<Order.Domain.Entities.Order, OrderView>, OrderViewMapper>();
        services.AddTracedScoped<IOrderCommandService, OrderCommandService>();
        services.AddTracedScoped<IOrderQueryService, OrderQueryService>();

        return services;
    }
}
