using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shipping.Infrastructure.Persistence;

public sealed class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext>
{
    public ShippingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ShippingDb")
            ?? "Host=localhost;Port=5432;Database=shipping_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ShippingDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ShippingDbContext(optionsBuilder.Options);
    }
}
