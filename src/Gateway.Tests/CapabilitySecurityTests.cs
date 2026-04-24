using Gateway.Api.Security;
using Xunit;

namespace Gateway.Tests;

public sealed class CapabilitySecurityTests
{
    [Fact]
    public void IsSatisfiedBy_ExactCapability_ReturnsTrue()
    {
        var result = CapabilitySecurity.IsSatisfiedBy(
            [CapabilitySecurity.CatalogProductsRead],
            CapabilitySecurity.CatalogProductsRead);

        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_NamespaceWildcard_ReturnsTrue()
    {
        var result = CapabilitySecurity.IsSatisfiedBy(
            ["catalog.*"],
            CapabilitySecurity.CatalogProductsWrite);

        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_MissingCapability_ReturnsFalse()
    {
        var result = CapabilitySecurity.IsSatisfiedBy(
            [CapabilitySecurity.CatalogProductsRead],
            CapabilitySecurity.WarehouseStockWrite);

        Assert.False(result);
    }
}
