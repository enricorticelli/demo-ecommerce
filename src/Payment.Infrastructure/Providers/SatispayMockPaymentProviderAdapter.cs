using Microsoft.Extensions.Configuration;
using Payment.Application.Services;

namespace Payment.Infrastructure.Providers;

public sealed class SatispayMockPaymentProviderAdapter(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory)
    : MockPaymentProviderAdapterBase(configuration, httpClientFactory)
{
    private static readonly IReadOnlySet<string> PaymentMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        "satispay"
    };

    public override string ProviderCode => "satispay";
    protected override IReadOnlySet<string> SupportedPaymentMethods => PaymentMethods;

    protected override PaymentWebhookDecision MapDecision(string providerStatus)
    {
        var normalized = providerStatus.Trim().ToUpperInvariant();
        return normalized switch
        {
            "ACCEPTED" => PaymentWebhookDecision.Authorized,
            "CANCELED" => PaymentWebhookDecision.Rejected,
            "CANCELLED" => PaymentWebhookDecision.Rejected,
            "REJECTED" => PaymentWebhookDecision.Rejected,
            _ => PaymentWebhookDecision.Pending
        };
    }
}

