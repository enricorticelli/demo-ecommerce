using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shipping.Infrastructure.Persistence;

namespace Shipping.Infrastructure.Configuration;

public static class ShippingApplicationExtensions
{
    public static async Task UseShippingModuleAsync(this WebApplication app)
    {
        var options = ShippingTechnicalOptions.FromConfiguration(app.Configuration);
        if (options.SkipWolverineBootstrap)
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShippingDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
