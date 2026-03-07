using Order.Api.Endpoints;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultApiServices();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapOrderEndpoints();

await app.RunAsync();
