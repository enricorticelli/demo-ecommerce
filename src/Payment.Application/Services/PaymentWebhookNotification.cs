namespace Payment.Application.Services;

public sealed record PaymentWebhookNotification(
    string ExternalEventId,
    Guid? SessionId,
    string? ExternalCheckoutId,
    string ProviderStatus,
    PaymentWebhookDecision Decision,
    string? TransactionId,
    string? FailureReason);

