using Payment.Application.Abstractions.Providers;
using Payment.Infrastructure.Providers;
using Xunit;

namespace Payment.Tests;

public sealed class PaymentProviderRouterTests
{
    [Fact]
    public void ResolveByPaymentMethod_ProviderExists_ReturnsMatchingProvider()
    {
        var stripeClient = new StubProviderClient("stripe", new HashSet<string>() { "stripe_card" });
        var paypalClient = new StubProviderClient("paypal", new HashSet<string>() { "paypal" });
        var satispayClient = new StubProviderClient("satispay", new HashSet<string>() { "satispay" });

        var router = new PaymentProviderRouter(
            [stripeClient, paypalClient, satispayClient],
            [new StubWebhookVerifier("stripe"), new StubWebhookVerifier("paypal"), new StubWebhookVerifier("satispay")]);

        var provider = router.ResolveByPaymentMethod("paypal");

        Assert.Equal("paypal", provider.ProviderCode);
    }

    private sealed class StubProviderClient(string providerCode, IReadOnlySet<string> paymentMethods) : IPaymentProviderClient
    {
        public string ProviderCode => providerCode;
        public bool SupportsPaymentMethod(string paymentMethod) => paymentMethods.Contains(paymentMethod);
        public Task<Payment.Application.Services.PaymentProviderCheckoutResult> CreateCheckoutAsync(
            Payment.Application.Services.PaymentProviderCheckoutRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Payment.Application.Services.PaymentWebhookNotification ParseWebhook(string rawPayload) => throw new NotSupportedException();
    }

    private sealed class StubWebhookVerifier(string providerCode) : IPaymentWebhookVerifier
    {
        public string ProviderCode => providerCode;
        public bool VerifySignature(string rawPayload, IReadOnlyDictionary<string, string> headers) => true;
    }
}


