using Payment.Application.Abstractions.Providers;
using Shared.BuildingBlocks.Exceptions;

namespace Payment.Infrastructure.Providers;

public sealed class PaymentProviderRouter(
    IEnumerable<IPaymentProviderClient> providerClients,
    IEnumerable<IPaymentWebhookVerifier> webhookVerifiers) : IPaymentProviderRouter
{
    private readonly IPaymentProviderClient[] _providerClients = providerClients.ToArray();
    private readonly Dictionary<string, IPaymentWebhookVerifier> _webhookVerifiers = webhookVerifiers
        .GroupBy(x => x.ProviderCode.Trim().ToLowerInvariant(), StringComparer.Ordinal)
        .ToDictionary(x => x.Key, x => x.Last(), StringComparer.Ordinal);

    public IPaymentProviderClient ResolveByPaymentMethod(string paymentMethod)
    {
        var normalizedPaymentMethod = string.IsNullOrWhiteSpace(paymentMethod)
            ? string.Empty
            : paymentMethod.Trim().ToLowerInvariant();

        var client = _providerClients.FirstOrDefault(x => x.SupportsPaymentMethod(normalizedPaymentMethod));
        if (client is null)
        {
            throw new ValidationAppException($"Unsupported payment method '{paymentMethod}'.");
        }

        return client;
    }

    public IPaymentProviderClient ResolveByProviderCode(string providerCode)
    {
        var normalizedProviderCode = string.IsNullOrWhiteSpace(providerCode)
            ? string.Empty
            : providerCode.Trim().ToLowerInvariant();

        var client = _providerClients.FirstOrDefault(x =>
            string.Equals(x.ProviderCode, normalizedProviderCode, StringComparison.OrdinalIgnoreCase));

        if (client is null)
        {
            throw new ValidationAppException($"Unsupported payment provider '{providerCode}'.");
        }

        return client;
    }

    public IPaymentWebhookVerifier ResolveWebhookVerifier(string providerCode)
    {
        var normalizedProviderCode = string.IsNullOrWhiteSpace(providerCode)
            ? string.Empty
            : providerCode.Trim().ToLowerInvariant();

        if (!_webhookVerifiers.TryGetValue(normalizedProviderCode, out var verifier))
        {
            throw new ValidationAppException($"Unsupported payment provider '{providerCode}'.");
        }

        return verifier;
    }
}

