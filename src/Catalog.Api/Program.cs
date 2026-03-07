using Catalog.Api.Endpoints;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultApiServices();

var app = builder.Build();

app.UseDefaultApiPipeline();

app.MapProductEndpoints();
app.MapCategoryEndpoints();
app.MapBrandEndpoints();
app.MapCollectionEndpoints();

await app.RunAsync();
