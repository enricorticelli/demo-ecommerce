using Payment.Api.Endpoints;
using Payment.Application.Composition;
using Payment.Infrastructure.Composition;
using Shared.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultApiServices();

builder.Services.AddPaymentApplication();
builder.AddPaymentInfrastructure();

var app = builder.Build();

app.UseDefaultApiPipeline();
app.MapPaymentEndpoints();

await app.RunAsync();
