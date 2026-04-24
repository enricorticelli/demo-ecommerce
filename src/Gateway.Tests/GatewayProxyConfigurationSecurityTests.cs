using Gateway.Api.Configuration;
using Gateway.Api.Security;
using Xunit;

namespace Gateway.Tests;

public sealed class GatewayProxyConfigurationSecurityTests
{
    [Fact]
    public void CreateRoutes_BackofficeRoutes_RequireCapabilityPolicies()
    {
        var routes = GatewayProxyConfiguration.CreateRoutes();

        var productReadRoute = routes.Single(route =>
            route.RouteId == "backoffice-catalog-products-collection-read-route");
        var productWriteRoute = routes.Single(route =>
            route.RouteId == "backoffice-catalog-products-collection-write-route");
        var warehouseWriteRoute = routes.Single(route =>
            route.RouteId == "backoffice-warehouse-upsert-route");

        Assert.Equal(
            CapabilitySecurity.PolicyName(CapabilitySecurity.CatalogProductsRead),
            productReadRoute.AuthorizationPolicy);
        Assert.Equal(
            CapabilitySecurity.PolicyName(CapabilitySecurity.CatalogProductsWrite),
            productWriteRoute.AuthorizationPolicy);
        Assert.Equal(
            CapabilitySecurity.PolicyName(CapabilitySecurity.WarehouseStockWrite),
            warehouseWriteRoute.AuthorizationPolicy);
    }

    [Fact]
    public void CreateRoutes_StoreAndWebhookRoutes_RemainPublic()
    {
        var routes = GatewayProxyConfiguration.CreateRoutes();

        var publicRoutes = routes.Where(route =>
            route.Match?.Path?.StartsWith("/api/store/", StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();

        Assert.NotEmpty(publicRoutes);
        Assert.All(publicRoutes, route => Assert.Null(route.AuthorizationPolicy));
    }
}
