using Catalog.Api.Endpoints;
using Catalog.Application.Composition;
using Catalog.Infrastructure.Composition;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultApiServices();

builder.Services.AddCatalogApplication();
builder.AddCatalogInfrastructure();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapCatalogEndpoints();

await app.RunAsync();
