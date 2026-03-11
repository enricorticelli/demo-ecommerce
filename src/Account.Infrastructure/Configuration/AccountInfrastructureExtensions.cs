using Account.Application.Abstractions.Services;
using Account.Application.Models;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Account.Infrastructure.Configuration;

public static class AccountInfrastructureExtensions
{
    public static WebApplicationBuilder AddAccountModule(this WebApplicationBuilder builder)
    {
        var options = AccountTechnicalOptions.FromConfiguration(builder.Configuration);

        builder.Services.AddSingleton(options);
        builder.Services.AddDbContext<AccountDbContext>(db => db.UseNpgsql(options.AccountConnectionString));

        builder.Services.AddScoped<IAccountAuthService, AccountAuthService>();
        builder.Services.AddScoped<IAccountCustomerProfileService, AccountCustomerProfileService>();
        builder.Services.AddScoped<IAccountAdministrationService, AccountAdministrationService>();
        builder.Services.AddScoped<IAccountBootstrapService, AccountBootstrapService>();
        builder.Services.AddScoped<TokenFactory>();

        builder.Services.AddHttpClient<OrderApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.OrderApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.TokenValidationParameters = new TokenFactory(options).CreateValidationParameters();
            });

        builder.Services.AddAuthorization(optionsAuth =>
        {
            optionsAuth.AddPolicy("CustomerPolicy", policy => policy.RequireClaim("realm", AccountRealm.Customer));
            optionsAuth.AddPolicy("AdminPolicy", policy => policy.RequireClaim("realm", AccountRealm.Admin));
        });

        return builder;
    }

    public static async Task UseAccountModuleAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var dbContext = services.GetRequiredService<AccountDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var options = services.GetRequiredService<AccountTechnicalOptions>();
        var bootstrapService = services.GetRequiredService<IAccountBootstrapService>();
        await bootstrapService.EnsureDefaultAdminAsync(options.DefaultAdminUsername, options.DefaultAdminPassword, CancellationToken.None);
    }
}
