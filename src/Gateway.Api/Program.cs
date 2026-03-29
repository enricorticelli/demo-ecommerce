using Gateway.Api.Configuration;
using Gateway.Api.Extensions;
using Gateway.Api.OpenApi;
using Shared.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();
builder.Services
    .AddGatewayCoreServices()
    .AddGatewayOpenApi();

var routes = GatewayProxyConfiguration.CreateRoutes();
var clusters = GatewayProxyConfiguration.CreateClusters();
builder.Services.AddGatewayReverseProxy(routes, clusters);

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("default");

app.UseGatewayOpenApi(routes);
app.MapGatewayOperationalEndpoints();

app.MapReverseProxy();
await app.RunAsync();
