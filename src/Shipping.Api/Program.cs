using Shared.BuildingBlocks.Api;
using Shipping.Api.Endpoints;
using Shipping.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultApiServices();
builder.AddStoreAndAdminAuthentication();
builder.AddShippingModule();

var app = builder.Build();
await app.UseShippingModuleAsync();

app.UseDefaultApiPipeline();
app.UseAuthentication();
app.UseAuthorization();
app.MapShippingEndpoints();

await app.RunAsync();
