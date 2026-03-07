using Shared.BuildingBlocks.Api;
using Warehouse.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultApiServices();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapWarehouseEndpoints();

await app.RunAsync();
