using Scalar.AspNetCore;
using Shared.BuildingBlocks.Observability;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "E-commerce Gateway API",
        Version = "v1"
    });

    options.SwaggerDoc("store", new()
    {
        Title = "E-commerce Gateway Store API",
        Version = "v1"
    });

    options.SwaggerDoc("backoffice", new()
    {
        Title = "E-commerce Gateway Backoffice API",
        Version = "v1"
    });

    options.DocInclusionPredicate(static (documentName, apiDescription) =>
    {
        var relativePath = apiDescription.RelativePath;
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var normalizedPath = "/" + relativePath.TrimStart('/');

        return documentName switch
        {
            "store" => normalizedPath.StartsWith("/api/store/", StringComparison.OrdinalIgnoreCase),
            "backoffice" => normalizedPath.StartsWith("/api/backoffice/", StringComparison.OrdinalIgnoreCase),
            "v1" => true,
            _ => false
        };
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var routes = new List<RouteConfig>
{
    // store/catalog
    CreateContextRoute("store-catalog-products-collection-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products", "GET"),
    CreateContextRoute("store-catalog-products-item-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products/{id}", "GET"),
    CreateContextRoute("store-catalog-products-new-arrivals-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products/new-arrivals", "GET"),
    CreateContextRoute("store-catalog-products-best-sellers-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products/best-sellers", "GET"),

    // backoffice/catalog
    CreateContextRoute("backoffice-catalog-products-collection-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/products", "GET", "POST"),
    CreateContextRoute("backoffice-catalog-products-item-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/products/{id}", "GET", "PUT", "DELETE"),
    CreateContextRoute("backoffice-catalog-brands-collection-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/brands", "GET", "POST"),
    CreateContextRoute("backoffice-catalog-brands-item-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/brands/{id}", "GET", "PUT", "DELETE"),
    CreateContextRoute("backoffice-catalog-categories-collection-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/categories", "GET", "POST"),
    CreateContextRoute("backoffice-catalog-categories-item-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/categories/{id}", "GET", "PUT", "DELETE"),
    CreateContextRoute("backoffice-catalog-collections-collection-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/collections", "GET", "POST"),
    CreateContextRoute("backoffice-catalog-collections-item-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/collections/{id}", "GET", "PUT", "DELETE"),

    // store/cart
    CreateContextRoute("store-cart-get-route", "cart-cluster", "store", "cart", "/api/store/cart/v1/carts/{cartId}", "GET"),
    CreateContextRoute("store-cart-add-item-route", "cart-cluster", "store", "cart", "/api/store/cart/v1/carts/{cartId}/items", "POST"),
    CreateContextRoute("store-cart-remove-item-route", "cart-cluster", "store", "cart", "/api/store/cart/v1/carts/{cartId}/items/{productId}", "DELETE"),

    // store/order
    CreateContextRoute("store-order-create-route", "order-cluster", "store", "order", "/api/store/order/v1/orders", "POST"),
    CreateContextRoute("store-order-list-route", "order-cluster", "store", "order", "/api/store/order/v1/orders", "GET"),
    CreateContextRoute("store-order-get-route", "order-cluster", "store", "order", "/api/store/order/v1/orders/{orderId}", "GET"),
    CreateContextRoute("store-order-manual-cancel-route", "order-cluster", "store", "order", "/api/store/order/v1/orders/{orderId}/manual-cancel", "POST"),

    // store/payment
    CreateContextRoute("store-payment-session-by-order-route", "payment-cluster", "store", "payment", "/api/store/payment/v1/payments/sessions/orders/{orderId}", "GET"),
    CreateContextRoute("store-payment-session-by-id-route", "payment-cluster", "store", "payment", "/api/store/payment/v1/payments/sessions/{sessionId}", "GET"),
    CreateContextRoute("store-payment-webhook-stripe-route", "payment-cluster", "store", "payment", "/api/store/payment/v1/payments/webhooks/stripe", "POST"),
    CreateContextRoute("store-payment-webhook-paypal-route", "payment-cluster", "store", "payment", "/api/store/payment/v1/payments/webhooks/paypal", "POST"),
    CreateContextRoute("store-payment-webhook-satispay-route", "payment-cluster", "store", "payment", "/api/store/payment/v1/payments/webhooks/satispay", "POST"),

    // store/shipping
    CreateContextRoute("store-shipping-by-order-route", "shipping-cluster", "store", "shipping", "/api/store/shipping/v1/shipments/orders/{orderId}", "GET"),

    // backoffice/warehouse
    CreateContextRoute("backoffice-warehouse-query-route", "warehouse-cluster", "backoffice", "warehouse", "/api/backoffice/warehouse/v1/stock/query", "POST"),
    CreateContextRoute("backoffice-warehouse-upsert-route", "warehouse-cluster", "backoffice", "warehouse", "/api/backoffice/warehouse/v1/stock", "POST")
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
    options.WithTitle("E-commerce Gateway API");
});

app.MapGet("/v1/system/info", () => TypedResults.Ok(new
{
    Name = "ecommerce-gateway",
    Version = "v1",
    Timestamp = DateTimeOffset.UtcNow
}))
.WithName("GetSystemInfo")
.WithTags("System");

app.MapReverseProxy();
await app.RunAsync();

static RouteConfig CreateContextRoute(
    string routeId,
    string clusterId,
    string context,
    string service,
    string matchPath,
    params string[] methods)
{
    var route = new RouteConfig
    {
        RouteId = routeId,
        ClusterId = clusterId,
        Match = new RouteMatch { Path = matchPath, Methods = methods },
        Transforms =
        [
            new Dictionary<string, string> { ["PathRemovePrefix"] = $"/api/{context}/{service}" },
            new Dictionary<string, string> { ["PathPrefix"] = $"/{context}" }
        ]
    };

    return route;
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
