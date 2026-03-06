using Shared.BuildingBlocks.Api;
using User.Api.Endpoints;
using User.Application.Composition;
using User.Infrastructure.Composition;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultApiServices();

builder.Services.AddUserApplication();
builder.AddUserInfrastructure();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapUserEndpoints();

await app.RunAsync();
