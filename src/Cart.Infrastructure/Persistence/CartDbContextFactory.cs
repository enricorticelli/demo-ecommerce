using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cart.Infrastructure.Persistence;

public sealed class CartDbContextFactory : IDesignTimeDbContextFactory<CartDbContext>
{
    public CartDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CartDb")
            ?? "Host=localhost;Port=5432;Database=cart_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<CartDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new CartDbContext(optionsBuilder.Options);
    }
}
