using Shared.BuildingBlocks.Api;
using Shipping.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultApiServices();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapShippingEndpoints();

await app.RunAsync();
