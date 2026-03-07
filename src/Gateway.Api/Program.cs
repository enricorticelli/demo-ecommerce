using Scalar.AspNetCore;
using Shared.BuildingBlocks.Observability;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var routes = new List<RouteConfig>
{
    CreateRoute("catalog-route", "catalog-cluster", "/api/catalog/{**catch-all}", "/api/catalog"),
    CreateRoute("cart-route", "cart-cluster", "/api/cart/{**catch-all}", "/api/cart"),
    CreateRoute("order-route", "order-cluster", "/api/order/{**catch-all}", "/api/order"),
    CreateRoute("payment-route", "payment-cluster", "/api/payment/{**catch-all}", "/api/payment"),
    CreateRoute("warehouse-route", "warehouse-cluster", "/api/warehouse/{**catch-all}", "/api/warehouse"),
    CreateRoute("shipping-route", "shipping-cluster", "/api/shipping/{**catch-all}", "/api/shipping")
};

var clusters = new List<ClusterConfig>
{
    CreateCluster("catalog-cluster", "http://catalog-api:8080/"),
    CreateCluster("cart-cluster", "http://cart-api:8080/"),
    CreateCluster("order-cluster", "http://order-api:8080/"),
    CreateCluster("payment-cluster", "http://payment-api:8080/"),
    CreateCluster("warehouse-cluster", "http://warehouse-api:8080/"),
    CreateCluster("shipping-cluster", "http://shipping-api:8080/")
};

builder.Services.AddReverseProxy().LoadFromMemory(routes, clusters);

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("default");

app.UseSwagger(options =>
{
    options.RouteTemplate = "openapi/{documentName}.json";
});


app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapScalarApiReference("/scalar", options =>
{
    options.WithTitle("CQRS E-commerce Gateway API");
});

app.MapGet("/v1/system/info", () => TypedResults.Ok(new
{
    Name = "cqrs-ecommerce-gateway",
    Version = "v1",
    Timestamp = DateTimeOffset.UtcNow
}))
.WithName("GetSystemInfo")
.WithTags("System");

app.MapReverseProxy();
await app.RunAsync();

static RouteConfig CreateRoute(string routeId, string clusterId, string matchPath, string removePrefix)
{
    return new RouteConfig
    {
        RouteId = routeId,
        ClusterId = clusterId,
        Match = new RouteMatch { Path = matchPath },
        Transforms =
        [
            new Dictionary<string, string> { ["PathRemovePrefix"] = removePrefix }
        ]
    };
}

static ClusterConfig CreateCluster(string clusterId, string destinationAddress)
{
    return new ClusterConfig
    {
        ClusterId = clusterId,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["destination"] = new() { Address = destinationAddress }
        }
    };
}
