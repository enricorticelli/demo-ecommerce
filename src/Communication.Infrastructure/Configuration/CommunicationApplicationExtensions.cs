using Communication.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Communication.Infrastructure.Configuration;

public static class CommunicationApplicationExtensions
{
    public static async Task UseCommunicationModuleAsync(this WebApplication app)
    {
        var options = CommunicationTechnicalOptions.FromConfiguration(app.Configuration);
        if (options.SkipWolverineBootstrap)
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
