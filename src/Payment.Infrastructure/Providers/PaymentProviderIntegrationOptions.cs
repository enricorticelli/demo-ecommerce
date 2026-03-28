using Microsoft.Extensions.Configuration;

namespace Payment.Infrastructure.Providers;

internal sealed record PaymentProviderIntegrationOptions(
    string CheckoutApiBaseUrl,
    string ApiKey,
    string WebhookSecret,
    string ReturnUrlTemplate,
    string CancelUrlTemplate)
{
    public static PaymentProviderIntegrationOptions FromConfiguration(IConfiguration configuration, string providerCode)
    {
        var normalizedProviderCode = providerCode.Trim().ToLowerInvariant();
        var providerSection = $"{char.ToUpperInvariant(normalizedProviderCode[0])}{normalizedProviderCode[1..]}";
        var prefix = $"Payment:Providers:{providerSection}";

        var checkoutApiBaseUrl = configuration[$"{prefix}:CheckoutApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(checkoutApiBaseUrl))
        {
            throw new InvalidOperationException($"Missing checkout API base URL for provider '{providerCode}'.");
        }

        var apiKey = configuration[$"{prefix}:ApiKey"] ?? string.Empty;
        var webhookSecret = configuration[$"{prefix}:WebhookSecret"] ?? string.Empty;

        var defaultReturnTemplate = "http://localhost:3000/payment/return?orderId={orderId}&sessionId={sessionId}&provider={provider}&result=authorized";
        var defaultCancelTemplate = "http://localhost:3000/payment/return?orderId={orderId}&sessionId={sessionId}&provider={provider}&result=cancelled";

        var returnUrlTemplate = configuration[$"{prefix}:ReturnUrlTemplate"] ?? defaultReturnTemplate;
        var cancelUrlTemplate = configuration[$"{prefix}:CancelUrlTemplate"] ?? defaultCancelTemplate;

        return new PaymentProviderIntegrationOptions(
            checkoutApiBaseUrl.TrimEnd('/'),
            apiKey.Trim(),
            webhookSecret.Trim(),
            returnUrlTemplate,
            cancelUrlTemplate);
    }
}

