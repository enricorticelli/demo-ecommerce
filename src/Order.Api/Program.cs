using Order.Api.Endpoints;
using Order.Application.Composition;
using Order.Infrastructure.Composition;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultApiServices();

builder.Services.AddOrderApplication();
builder.AddOrderInfrastructure();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapOrderEndpoints();

await app.RunAsync();
