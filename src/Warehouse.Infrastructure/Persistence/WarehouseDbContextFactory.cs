using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Warehouse.Infrastructure.Persistence;

public sealed class WarehouseDbContextFactory : IDesignTimeDbContextFactory<WarehouseDbContext>
{
    public WarehouseDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__WarehouseDb")
            ?? "Host=localhost;Port=5432;Database=warehouse_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<WarehouseDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new WarehouseDbContext(optionsBuilder.Options);
    }
}
