using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.Infrastructure.Persistence;

namespace Order.Infrastructure.Configuration;

public static class OrderApplicationExtensions
{
    public static async Task UseOrderModuleAsync(this WebApplication app)
    {
        var options = OrderTechnicalOptions.FromConfiguration(app.Configuration);
        if (options.SkipWolverineBootstrap)
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
