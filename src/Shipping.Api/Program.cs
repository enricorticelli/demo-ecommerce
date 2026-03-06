using Shared.BuildingBlocks.Api;
using Shipping.Api.Endpoints;
using Shipping.Application.Composition;
using Shipping.Infrastructure.Composition;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultApiServices();

builder.Services.AddShippingApplication();
builder.AddShippingInfrastructure();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapShippingEndpoints();

await app.RunAsync();
