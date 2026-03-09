namespace Payment.Application.Abstractions.Providers;

public interface IPaymentProviderRouter
{
    IPaymentProviderClient ResolveByPaymentMethod(string paymentMethod);
    IPaymentProviderClient ResolveByProviderCode(string providerCode);
    IPaymentWebhookVerifier ResolveWebhookVerifier(string providerCode);
}

