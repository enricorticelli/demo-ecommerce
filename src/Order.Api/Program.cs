using Order.Api.Endpoints;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultApiServices();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapOrderEndpoints();

await app.RunAsync();
