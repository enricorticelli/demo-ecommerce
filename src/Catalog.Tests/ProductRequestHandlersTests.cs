using Catalog.Application;
using Moq;
using Xunit;

namespace Catalog.Tests;

public sealed class ProductRequestHandlersTests
{
    [Fact]
    public async Task Create_product_command_should_delegate_to_catalog_service()
    {
        var service = new Mock<ICatalogService>();
        var payload = new CreateProductCommand("SKU-1", "P", "D", 10m, Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], true, false);
        var expected = new ProductView(Guid.NewGuid(), "SKU-1", "P", "D", 10m, Guid.NewGuid(), "B", Guid.NewGuid(), "C", [], [], true, false, DateTimeOffset.UtcNow);

        service.Setup(x => x.CreateProductAsync(payload, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var sut = new ProductRequestHandlers(service.Object);

        var actual = await sut.HandleAsync(new CreateProductCatalogCommand(payload), CancellationToken.None);

        Assert.Equal(expected, actual);
    }
}
