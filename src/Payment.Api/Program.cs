using Payment.Api.Endpoints;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultApiServices();
var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapPaymentEndpoints();

await app.RunAsync();
