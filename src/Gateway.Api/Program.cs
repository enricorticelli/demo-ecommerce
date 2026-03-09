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
    // store/catalog
    CreateContextRoute("store-catalog-products-list-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products", "GET"),
    CreateContextRoute("store-catalog-products-new-arrivals-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products/new-arrivals", "GET"),
    CreateContextRoute("store-catalog-products-best-sellers-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products/best-sellers", "GET"),
    CreateContextRoute("store-catalog-products-by-id-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products/{id}", "GET"),

    // store/cart
    CreateContextRoute("store-cart-get-route", "cart-cluster", "store", "cart", "/api/store/cart/v1/carts/{cartId}", "GET"),
    CreateContextRoute("store-cart-add-item-route", "cart-cluster", "store", "cart", "/api/store/cart/v1/carts/{cartId}/items", "POST"),
    CreateContextRoute("store-cart-remove-item-route", "cart-cluster", "store", "cart", "/api/store/cart/v1/carts/{cartId}/items/{productId}", "DELETE"),

    // store/order
    CreateContextRoute("store-order-create-route", "order-cluster", "store", "order", "/api/store/order/v1/orders", "POST"),
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

    // admin/catalog
    CreateContextRoute("admin-catalog-products-collection-route", "catalog-cluster", "admin", "catalog", "/api/admin/catalog/v1/products", "GET", "POST"),
    CreateContextRoute("admin-catalog-products-item-route", "catalog-cluster", "admin", "catalog", "/api/admin/catalog/v1/products/{id}", "GET", "PUT", "DELETE"),
    CreateContextRoute("admin-catalog-brands-collection-route", "catalog-cluster", "admin", "catalog", "/api/admin/catalog/v1/brands", "GET", "POST"),
    CreateContextRoute("admin-catalog-brands-item-route", "catalog-cluster", "admin", "catalog", "/api/admin/catalog/v1/brands/{id}", "GET", "PUT", "DELETE"),
    CreateContextRoute("admin-catalog-categories-collection-route", "catalog-cluster", "admin", "catalog", "/api/admin/catalog/v1/categories", "GET", "POST"),
    CreateContextRoute("admin-catalog-categories-item-route", "catalog-cluster", "admin", "catalog", "/api/admin/catalog/v1/categories/{id}", "GET", "PUT", "DELETE"),
    CreateContextRoute("admin-catalog-collections-collection-route", "catalog-cluster", "admin", "catalog", "/api/admin/catalog/v1/collections", "GET", "POST"),
    CreateContextRoute("admin-catalog-collections-item-route", "catalog-cluster", "admin", "catalog", "/api/admin/catalog/v1/collections/{id}", "GET", "PUT", "DELETE"),

    // admin/order
    CreateContextRoute("admin-order-list-route", "order-cluster", "admin", "order", "/api/admin/order/v1/orders", "GET"),
    CreateContextRoute("admin-order-get-route", "order-cluster", "admin", "order", "/api/admin/order/v1/orders/{orderId}", "GET"),
    CreateContextRoute("admin-order-manual-complete-route", "order-cluster", "admin", "order", "/api/admin/order/v1/orders/{orderId}/manual-complete", "POST"),
    CreateContextRoute("admin-order-manual-cancel-route", "order-cluster", "admin", "order", "/api/admin/order/v1/orders/{orderId}/manual-cancel", "POST"),

    // admin/shipping
    CreateContextRoute("admin-shipping-list-route", "shipping-cluster", "admin", "shipping", "/api/admin/shipping/v1/shipments", "GET"),
    CreateContextRoute("admin-shipping-by-order-route", "shipping-cluster", "admin", "shipping", "/api/admin/shipping/v1/shipments/orders/{orderId}", "GET"),
    CreateContextRoute("admin-shipping-update-status-route", "shipping-cluster", "admin", "shipping", "/api/admin/shipping/v1/shipments/{shipmentId}/status", "POST"),

    // admin/warehouse
    CreateContextRoute("admin-warehouse-upsert-stock-route", "warehouse-cluster", "admin", "warehouse", "/api/admin/warehouse/v1/stock", "POST")
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
