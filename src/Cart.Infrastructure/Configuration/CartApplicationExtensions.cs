using Cart.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.Infrastructure.Configuration;

public static class CartApplicationExtensions
{
    public static async Task UseCartModuleAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
