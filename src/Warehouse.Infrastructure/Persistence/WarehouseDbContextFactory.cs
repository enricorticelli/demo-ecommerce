using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.BuildingBlocks.Configuration;

namespace Warehouse.Infrastructure.Persistence;

public sealed class WarehouseDbContextFactory : IDesignTimeDbContextFactory<WarehouseDbContext>
{
    public WarehouseDbContext CreateDbContext(string[] args)
    {
        var connectionString = EnvironmentVariableReader.ResolveRequired("ConnectionStrings__WarehouseDb");

        var optionsBuilder = new DbContextOptionsBuilder<WarehouseDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new WarehouseDbContext(optionsBuilder.Options);
    }
}
