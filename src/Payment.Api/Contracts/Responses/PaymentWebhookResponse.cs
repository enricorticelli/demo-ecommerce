namespace Payment.Api.Contracts.Responses;

public sealed record PaymentWebhookResponse(string ProviderCode, string Status, string Message);

