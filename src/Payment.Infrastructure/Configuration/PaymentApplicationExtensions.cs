using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Configuration;

public static class PaymentApplicationExtensions
{
    public static async Task UsePaymentModuleAsync(this WebApplication app)
    {
        var options = PaymentTechnicalOptions.FromConfiguration(app.Configuration);
        if (options.SkipWolverineBootstrap)
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
