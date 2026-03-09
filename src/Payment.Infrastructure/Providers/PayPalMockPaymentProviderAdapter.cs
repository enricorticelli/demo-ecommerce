using Microsoft.Extensions.Configuration;
using Payment.Application.Services;

namespace Payment.Infrastructure.Providers;

public sealed class PayPalMockPaymentProviderAdapter(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory)
    : MockPaymentProviderAdapterBase(configuration, httpClientFactory)
{
    private static readonly IReadOnlySet<string> PaymentMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        "paypal"
    };

    public override string ProviderCode => "paypal";
    protected override IReadOnlySet<string> SupportedPaymentMethods => PaymentMethods;

    protected override PaymentWebhookDecision MapDecision(string providerStatus)
    {
        var normalized = providerStatus.Trim().ToUpperInvariant();
        return normalized switch
        {
            "CHECKOUT.ORDER.APPROVED" => PaymentWebhookDecision.Authorized,
            "PAYMENT.CAPTURE.COMPLETED" => PaymentWebhookDecision.Authorized,
            "CHECKOUT.ORDER.CANCELLED" => PaymentWebhookDecision.Rejected,
            "CHECKOUT.ORDER.VOIDED" => PaymentWebhookDecision.Rejected,
            "PAYMENT.CAPTURE.DENIED" => PaymentWebhookDecision.Rejected,
            _ => PaymentWebhookDecision.Pending
        };
    }
}

