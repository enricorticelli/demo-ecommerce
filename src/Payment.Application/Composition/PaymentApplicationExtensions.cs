using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application.Composition;

public static class PaymentApplicationExtensions
{
    public static IServiceCollection AddPaymentApplication(this IServiceCollection services)
    {
        return services.AddModuleApplication(typeof(PaymentApplicationExtensions).Assembly);
    }
}
