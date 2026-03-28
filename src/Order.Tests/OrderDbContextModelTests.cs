using Microsoft.EntityFrameworkCore;
using Order.Infrastructure.Persistence;
using Xunit;

namespace Order.Tests;

public sealed class OrderDbContextModelTests
{
    [Fact]
    public void Model_WhenBuilt_DefinesOrderTable()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase("order-model-test")
            .Options;

        using var context = new OrderDbContext(options);
        var model = context.Model;

        var entityType = model.FindEntityType("Order.Domain.Entities.Order");
        Assert.NotNull(entityType);
        Assert.Equal("orders", entityType!.GetTableName());
    }
}

