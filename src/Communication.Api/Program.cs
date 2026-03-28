using Communication.Infrastructure.Configuration;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultApiServices();
builder.AddCommunicationModule();

var app = builder.Build();
await app.UseCommunicationModuleAsync();

app.UseDefaultApiPipeline();

await app.RunAsync();
