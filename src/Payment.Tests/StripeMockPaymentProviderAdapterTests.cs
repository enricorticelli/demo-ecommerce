using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using Payment.Application.Services;
using Payment.Infrastructure.Providers;
using Xunit;

namespace Payment.Tests;

public sealed class StripeMockPaymentProviderAdapterTests
{
    [Fact]
    public async Task CreateCheckoutAsync_MockGatewayReturnsCheckout_ReturnsRedirectUrl()
    {
        var config = BuildConfiguration();
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"providerCode\":\"stripe\",\"externalCheckoutId\":\"chk_123\",\"redirectUrl\":\"http://localhost:8082/checkout/stripe/chk_123\",\"status\":\"created\"}",
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        var sut = new StripeMockPaymentProviderAdapter(config, httpClientFactory.Object);

        var result = await sut.CreateCheckoutAsync(
            new PaymentProviderCheckoutRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                99.9m,
                "stripe_card"),
            CancellationToken.None);

        Assert.Equal("stripe", result.ProviderCode);
        Assert.Equal("chk_123", result.ExternalCheckoutId);
        Assert.Equal("http://localhost:8082/checkout/stripe/chk_123", result.RedirectUrl);
    }

    [Fact]
    public void VerifySignature_ValidHmacHeader_ReturnsTrue()
    {
        var config = BuildConfiguration();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));

        var sut = new StripeMockPaymentProviderAdapter(config, httpClientFactory.Object);
        var payload = JsonSerializer.Serialize(new
        {
            eventId = Guid.NewGuid().ToString("N"),
            sessionId = Guid.NewGuid(),
            externalCheckoutId = "chk_123",
            status = "checkout.session.completed",
            transactionId = "TX-1"
        });

        var signature = ComputeHmac(payload, "secret_stripe");
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Mock-Signature"] = $"sha256={signature}"
        };

        var isValid = sut.VerifySignature(payload, headers);

        Assert.True(isValid);
    }

    [Fact]
    public void ParseWebhook_CompletedStatus_ReturnsAuthorizedDecision()
    {
        var config = BuildConfiguration();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));

        var sut = new StripeMockPaymentProviderAdapter(config, httpClientFactory.Object);
        var sessionId = Guid.NewGuid();
        var payload = $$"""
{
  "eventId":"evt_001",
  "sessionId":"{{sessionId}}",
  "externalCheckoutId":"chk_123",
  "status":"checkout.session.completed",
  "transactionId":"TX-1"
}
""";

        var parsed = sut.ParseWebhook(payload);

        Assert.Equal(PaymentWebhookDecision.Authorized, parsed.Decision);
        Assert.Equal(sessionId, parsed.SessionId);
        Assert.Equal("evt_001", parsed.ExternalEventId);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payment:Providers:Stripe:CheckoutApiBaseUrl"] = "http://payment-mock-gateway:8080/mock/stripe",
                ["Payment:Providers:Stripe:ApiKey"] = "stripe_test",
                ["Payment:Providers:Stripe:WebhookSecret"] = "secret_stripe",
                ["Payment:Providers:Stripe:ReturnUrlTemplate"] = "http://localhost:3000/payment/return?orderId={orderId}&sessionId={sessionId}&provider={provider}",
                ["Payment:Providers:Stripe:CancelUrlTemplate"] = "http://localhost:3000/payment/return?orderId={orderId}&sessionId={sessionId}&provider={provider}&result=cancelled"
            })
            .Build();
    }

    private static string ComputeHmac(string payload, string secret)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new System.Security.Cryptography.HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}


