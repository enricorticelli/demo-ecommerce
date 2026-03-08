using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Configuration;

public static class WarehouseApplicationExtensions
{
    public static async Task UseWarehouseModuleAsync(this WebApplication app)
    {
        var options = WarehouseTechnicalOptions.FromConfiguration(app.Configuration);
        if (options.SkipWolverineBootstrap)
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
