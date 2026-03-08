using Microsoft.AspNetCore.Builder;

namespace Cart.Infrastructure.Configuration;

public static class CartHostBuilderExtensions
{
    public static WebApplicationBuilder AddCartModule(this WebApplicationBuilder builder)
    {
        builder.Services.AddCartInfrastructure(builder.Configuration);
        return builder;
    }
}
