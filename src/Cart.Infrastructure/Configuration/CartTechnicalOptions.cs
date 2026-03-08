using Microsoft.Extensions.Configuration;

namespace Cart.Infrastructure.Configuration;

public sealed record CartTechnicalOptions(string CartConnectionString)
{
    public static CartTechnicalOptions FromConfiguration(IConfiguration configuration)
    {
        return new CartTechnicalOptions(ResolveCartConnectionString(configuration));
    }

    public static string ResolveCartConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings__CartDb"] ?? configuration.GetConnectionString("CartDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Cart connection string is missing.");
        }

        return connectionString;
    }
}
