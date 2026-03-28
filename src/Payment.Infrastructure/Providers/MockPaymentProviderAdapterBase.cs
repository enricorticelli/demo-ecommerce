using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Services;

namespace Payment.Infrastructure.Providers;

public abstract class MockPaymentProviderAdapterBase(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : IPaymentProviderClient, IPaymentWebhookVerifier
{
    public abstract string ProviderCode { get; }
    protected abstract IReadOnlySet<string> SupportedPaymentMethods { get; }

    public bool SupportsPaymentMethod(string paymentMethod)
    {
        var normalized = Normalize(paymentMethod);
        return SupportedPaymentMethods.Contains(normalized);
    }

    public async Task<PaymentProviderCheckoutResult> CreateCheckoutAsync(
        PaymentProviderCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var options = PaymentProviderIntegrationOptions.FromConfiguration(configuration, ProviderCode);

        var payload = new MockProviderCheckoutCreateRequest(
            request.SessionId,
            request.OrderId,
            request.UserId,
            request.Amount,
            request.PaymentMethod,
            ResolveTemplate(options.ReturnUrlTemplate, request, ProviderCode),
            ResolveTemplate(options.CancelUrlTemplate, request, ProviderCode));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{options.CheckoutApiBaseUrl}/checkouts")
        {
            Content = JsonContent.Create(payload)
        };

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            httpRequest.Headers.Add("X-Api-Key", options.ApiKey);
        }

        var client = httpClientFactory.CreateClient(nameof(MockPaymentProviderAdapterBase));
        using var response = await client.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var checkout = await response.Content.ReadFromJsonAsync<MockProviderCheckoutCreateResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException($"Provider '{ProviderCode}' returned an empty checkout response.");

        return new PaymentProviderCheckoutResult(
            Normalize(checkout.ProviderCode),
            checkout.ExternalCheckoutId,
            checkout.RedirectUrl,
            checkout.Status);
    }

    public bool VerifySignature(string rawPayload, IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("X-Mock-Signature", out var receivedSignature)
            || string.IsNullOrWhiteSpace(receivedSignature))
        {
            return false;
        }

        var options = PaymentProviderIntegrationOptions.FromConfiguration(configuration, ProviderCode);
        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
        {
            return false;
        }

        var normalizedReceivedSignature = NormalizeSignature(receivedSignature);
        var expectedSignature = ComputeHmac(rawPayload, options.WebhookSecret);
        if (normalizedReceivedSignature.Length != expectedSignature.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(normalizedReceivedSignature),
            Encoding.UTF8.GetBytes(expectedSignature));
    }

    public PaymentWebhookNotification ParseWebhook(string rawPayload)
    {
        var payload = JsonSerializer.Deserialize<MockProviderWebhookPayload>(rawPayload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Webhook payload cannot be parsed.");

        return new PaymentWebhookNotification(
            payload.EventId,
            payload.SessionId,
            payload.ExternalCheckoutId,
            payload.Status,
            MapDecision(payload.Status),
            payload.TransactionId,
            payload.Reason);
    }

    protected abstract PaymentWebhookDecision MapDecision(string providerStatus);

    private static string ResolveTemplate(string template, PaymentProviderCheckoutRequest request, string providerCode)
    {
        return template
            .Replace("{orderId}", request.OrderId.ToString(), StringComparison.Ordinal)
            .Replace("{sessionId}", request.SessionId.ToString(), StringComparison.Ordinal)
            .Replace("{provider}", providerCode, StringComparison.Ordinal);
    }

    private static string ComputeHmac(string payload, string secret)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(secretBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string NormalizeSignature(string signature)
    {
        return signature.Trim().ToLowerInvariant().Replace("sha256=", string.Empty, StringComparison.Ordinal);
    }

    protected static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Trim()
            .ToLowerInvariant()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}
