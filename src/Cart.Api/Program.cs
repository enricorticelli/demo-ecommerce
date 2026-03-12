using Cart.Api.Endpoints;
using Cart.Infrastructure.Configuration;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultApiServices();
builder.AddCustomerAuthentication();
builder.AddCartModule();

var app = builder.Build();
await app.UseCartModuleAsync();

app.UseDefaultApiPipeline();
app.UseAuthentication();
app.UseAuthorization();
app.MapCartEndpoints();

await app.RunAsync();
