using Shared.BuildingBlocks.Api;
using Warehouse.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultApiServices();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapWarehouseEndpoints();

await app.RunAsync();
