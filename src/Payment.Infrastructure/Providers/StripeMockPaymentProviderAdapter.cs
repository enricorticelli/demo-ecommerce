using Microsoft.Extensions.Configuration;
using Payment.Application.Services;

namespace Payment.Infrastructure.Providers;

public sealed class StripeMockPaymentProviderAdapter(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory)
    : MockPaymentProviderAdapterBase(configuration, httpClientFactory)
{
    private static readonly IReadOnlySet<string> PaymentMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        "stripe_card",
        "stripecard",
        "card",
        "creditcard"
    };

    public override string ProviderCode => "stripe";
    protected override IReadOnlySet<string> SupportedPaymentMethods => PaymentMethods;

    protected override PaymentWebhookDecision MapDecision(string providerStatus)
    {
        var normalized = Normalize(providerStatus);
        return normalized switch
        {
            "checkout.session.completed" => PaymentWebhookDecision.Authorized,
            "payment_intent.succeeded" => PaymentWebhookDecision.Authorized,
            "checkout.session.expired" => PaymentWebhookDecision.Rejected,
            "checkout.session.async_payment_failed" => PaymentWebhookDecision.Rejected,
            "payment_intent.payment_failed" => PaymentWebhookDecision.Rejected,
            _ => PaymentWebhookDecision.Pending
        };
    }
}
