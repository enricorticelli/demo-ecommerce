namespace Payment.Application.Abstractions.Providers;

public interface IPaymentWebhookVerifier
{
    string ProviderCode { get; }
    bool VerifySignature(string rawPayload, IReadOnlyDictionary<string, string> headers);
}

