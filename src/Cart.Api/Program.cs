using Cart.Api.Endpoints;
using Cart.Application.Composition;
using Cart.Infrastructure.Composition;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultApiServices();

builder.Services.AddCartApplication();
builder.AddCartInfrastructure();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapCartEndpoints();

await app.RunAsync();
