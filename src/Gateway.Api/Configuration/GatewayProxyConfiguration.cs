using Gateway.Api.Security;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Api.Configuration;

public static class GatewayProxyConfiguration
{
    public static IReadOnlyList<RouteConfig> CreateRoutes() =>
    [
        // store/catalog
        CreateContextRoute("store-catalog-products-collection-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products", "GET"),
        CreateContextRoute("store-catalog-products-item-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products/{id}", "GET"),
        CreateContextRoute("store-catalog-products-new-arrivals-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products/new-arrivals", "GET"),
        CreateContextRoute("store-catalog-products-best-sellers-route", "catalog-cluster", "store", "catalog", "/api/store/catalog/v1/products/best-sellers", "GET"),

        // backoffice/catalog
        CreateSecuredContextRoute("backoffice-catalog-products-collection-read-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/products", CapabilitySecurity.CatalogProductsRead, "GET"),
        CreateSecuredContextRoute("backoffice-catalog-products-collection-write-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/products", CapabilitySecurity.CatalogProductsWrite, "POST"),
        CreateSecuredContextRoute("backoffice-catalog-products-item-read-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/products/{id}", CapabilitySecurity.CatalogProductsRead, "GET"),
        CreateSecuredContextRoute("backoffice-catalog-products-item-write-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/products/{id}", CapabilitySecurity.CatalogProductsWrite, "PUT", "DELETE"),
        CreateSecuredContextRoute("backoffice-catalog-brands-collection-read-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/brands", CapabilitySecurity.CatalogBrandsRead, "GET"),
        CreateSecuredContextRoute("backoffice-catalog-brands-collection-write-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/brands", CapabilitySecurity.CatalogBrandsWrite, "POST"),
        CreateSecuredContextRoute("backoffice-catalog-brands-item-read-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/brands/{id}", CapabilitySecurity.CatalogBrandsRead, "GET"),
        CreateSecuredContextRoute("backoffice-catalog-brands-item-write-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/brands/{id}", CapabilitySecurity.CatalogBrandsWrite, "PUT", "DELETE"),
        CreateSecuredContextRoute("backoffice-catalog-categories-collection-read-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/categories", CapabilitySecurity.CatalogCategoriesRead, "GET"),
        CreateSecuredContextRoute("backoffice-catalog-categories-collection-write-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/categories", CapabilitySecurity.CatalogCategoriesWrite, "POST"),
        CreateSecuredContextRoute("backoffice-catalog-categories-item-read-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/categories/{id}", CapabilitySecurity.CatalogCategoriesRead, "GET"),
        CreateSecuredContextRoute("backoffice-catalog-categories-item-write-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/categories/{id}", CapabilitySecurity.CatalogCategoriesWrite, "PUT", "DELETE"),
        CreateSecuredContextRoute("backoffice-catalog-collections-collection-read-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/collections", CapabilitySecurity.CatalogCollectionsRead, "GET"),
        CreateSecuredContextRoute("backoffice-catalog-collections-collection-write-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/collections", CapabilitySecurity.CatalogCollectionsWrite, "POST"),
        CreateSecuredContextRoute("backoffice-catalog-collections-item-read-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/collections/{id}", CapabilitySecurity.CatalogCollectionsRead, "GET"),
        CreateSecuredContextRoute("backoffice-catalog-collections-item-write-route", "catalog-cluster", "backoffice", "catalog", "/api/backoffice/catalog/v1/collections/{id}", CapabilitySecurity.CatalogCollectionsWrite, "PUT", "DELETE"),

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
        CreateSecuredContextRoute("backoffice-warehouse-query-route", "warehouse-cluster", "backoffice", "warehouse", "/api/backoffice/warehouse/v1/stock/query", CapabilitySecurity.WarehouseStockRead, "POST"),
        CreateSecuredContextRoute("backoffice-warehouse-upsert-route", "warehouse-cluster", "backoffice", "warehouse", "/api/backoffice/warehouse/v1/stock", CapabilitySecurity.WarehouseStockWrite, "POST")
    ];

    public static IReadOnlyList<ClusterConfig> CreateClusters() =>
    [
        CreateCluster("catalog-cluster", "http://catalog-api:8080/"),
        CreateCluster("cart-cluster", "http://cart-api:8080/"),
        CreateCluster("order-cluster", "http://order-api:8080/"),
        CreateCluster("payment-cluster", "http://payment-api:8080/"),
        CreateCluster("warehouse-cluster", "http://warehouse-api:8080/"),
        CreateCluster("shipping-cluster", "http://shipping-api:8080/")
    ];

    private static RouteConfig CreateContextRoute(
        string routeId,
        string clusterId,
        string context,
        string service,
        string matchPath,
        params string[] methods)
    {
        return BuildContextRoute(routeId, clusterId, context, service, matchPath, authorizationPolicy: null, methods);
    }

    private static RouteConfig CreateSecuredContextRoute(
        string routeId,
        string clusterId,
        string context,
        string service,
        string matchPath,
        string requiredCapability,
        params string[] methods)
    {
        return BuildContextRoute(
            routeId,
            clusterId,
            context,
            service,
            matchPath,
            CapabilitySecurity.PolicyName(requiredCapability),
            methods);
    }

    private static RouteConfig BuildContextRoute(
        string routeId,
        string clusterId,
        string context,
        string service,
        string matchPath,
        string? authorizationPolicy,
        params string[] methods)
    {
        return new RouteConfig
        {
            RouteId = routeId,
            ClusterId = clusterId,
            AuthorizationPolicy = authorizationPolicy,
            Match = new RouteMatch { Path = matchPath, Methods = methods },
            Transforms =
            [
                new Dictionary<string, string> { ["PathRemovePrefix"] = $"/api/{context}/{service}" },
                new Dictionary<string, string> { ["PathPrefix"] = $"/{context}" }
            ]
        };
    }

    private static ClusterConfig CreateCluster(string clusterId, string destinationAddress)
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
}
