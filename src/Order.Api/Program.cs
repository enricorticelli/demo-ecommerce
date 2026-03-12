using Order.Api.Endpoints;
using Order.Infrastructure.Configuration;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultApiServices();
builder.AddStoreAndAdminAuthentication();
builder.AddOrderModule();

var app = builder.Build();
await app.UseOrderModuleAsync();

app.UseDefaultApiPipeline();
app.UseAuthentication();
app.UseAuthorization();
app.MapOrderEndpoints();

await app.RunAsync();
