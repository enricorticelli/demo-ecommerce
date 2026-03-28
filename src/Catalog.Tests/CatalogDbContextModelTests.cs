using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.Tests;

public sealed class CatalogDbContextModelTests
{
    [Fact]
    public void Model_WhenBuilt_DefinesExpectedTables()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase("catalog-model-test")
            .Options;

        using var context = new CatalogDbContext(options);
        var model = context.Model;

        Assert.NotNull(model.FindEntityType("Catalog.Domain.Entities.Brand")?.GetTableName());
        Assert.NotNull(model.FindEntityType("Catalog.Domain.Entities.Category")?.GetTableName());
        Assert.NotNull(model.FindEntityType("Catalog.Domain.Entities.Collection")?.GetTableName());
        Assert.NotNull(model.FindEntityType("Catalog.Domain.Entities.Product")?.GetTableName());
        Assert.NotNull(model.FindEntityType("Catalog.Domain.Entities.ProductCollection")?.GetTableName());
    }
}

