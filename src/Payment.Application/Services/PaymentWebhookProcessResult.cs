namespace Payment.Application.Services;

public sealed record PaymentWebhookProcessResult(PaymentWebhookProcessStatus Status, string Message);

