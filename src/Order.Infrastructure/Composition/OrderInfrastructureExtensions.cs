using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Order.Application;
using Order.Application.Abstractions;
using Order.Infrastructure.Messaging;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Persistence.ReadModels;
using Order.Infrastructure.WorkflowHandlers;
using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.Contracts.Integration;
using Shared.BuildingBlocks.Infrastructure;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Order.Infrastructure.Composition;

public static class OrderInfrastructureExtensions
{
    public static WebApplicationBuilder AddOrderInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddMarten(options =>
        {
            options.Connection(InfrastructureConnectionFactory.BuildPostgresConnectionString(
                Environment.GetEnvironmentVariable("ORDER_DB") ?? "orderdb"));
            options.DatabaseSchemaName = "ordering";
        });

        builder.Host.UseWolverine(options =>
        {
            options.UseRabbitMq(InfrastructureConnectionFactory.BuildRabbitMqConnectionString())
                .AutoProvision();

            options.ListenToRabbitQueue(IntegrationQueueNames.OrderWorkflow);
            options.PublishMessage<OrderPlacedV1>().ToRabbitQueue(IntegrationQueueNames.WarehouseWorkflow);
            options.PublishMessage<PaymentAuthorizeRequestedV1>().ToRabbitQueue(IntegrationQueueNames.PaymentWorkflow);
            options.PublishMessage<ShippingCreateRequestedV1>().ToRabbitQueue(IntegrationQueueNames.ShippingWorkflow);
            options.PublishMessage<OrderCompletedV1>().ToRabbitQueue(IntegrationQueueNames.CartWorkflow);

            options.Discovery.IncludeType<OrderStockReservedHandler>();
            options.Discovery.IncludeType<OrderStockRejectedHandler>();
            options.Discovery.IncludeType<OrderPaymentAuthorizedHandler>();
            options.Discovery.IncludeType<OrderPaymentFailedHandler>();
            options.Discovery.IncludeType<OrderShippingCreatedHandler>();
            options.Policies.AutoApplyTransactions();
        });

        builder.Services.AddScoped<IOrderReadModelStore, MongoOrderReadModelStore>();
        builder.Services.AddScoped<IOrderStateReader, OrderStateReader>();
        builder.Services.AddScoped<IOrderStateStore, MartenOrderStateStore>();
        builder.Services.AddScoped<IOrderEventPublisher, WolverineOrderEventPublisher>();
        builder.Services.AddScoped<IOrderWorkflowProcessor, OrderWorkflowProcessor>();
        builder.Services.AddScoped<IOrderCommandService, OrderCommandService>();
        builder.Services.AddScoped<IOrderQueryService, OrderReadService>();

        return builder;
    }
}
