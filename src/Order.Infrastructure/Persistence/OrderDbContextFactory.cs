using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Order.Infrastructure.Persistence;

public sealed class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__OrderDb")
            ?? "Host=localhost;Port=5432;Database=order_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new OrderDbContext(optionsBuilder.Options);
    }
}
