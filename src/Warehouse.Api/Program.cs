using Shared.BuildingBlocks.Api;
using Warehouse.Api.Endpoints;
using Warehouse.Application.Composition;
using Warehouse.Infrastructure.Composition;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultApiServices();

builder.Services.AddWarehouseApplication();
builder.AddWarehouseInfrastructure();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapWarehouseEndpoints();

await app.RunAsync();
