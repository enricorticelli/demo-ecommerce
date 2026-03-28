using Payment.Api.Endpoints;
using Payment.Infrastructure.Configuration;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultApiServices();
builder.AddPaymentModule();
var app = builder.Build();
await app.UsePaymentModuleAsync();

app.UseDefaultApiPipeline();
app.MapPaymentEndpoints();

await app.RunAsync();
