namespace Payment.Infrastructure.Persistence.Entities;

public sealed class ProcessedPaymentWebhookEvent
{
    public string ProviderCode { get; set; } = string.Empty;
    public string ExternalEventId { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAtUtc { get; set; }
}

