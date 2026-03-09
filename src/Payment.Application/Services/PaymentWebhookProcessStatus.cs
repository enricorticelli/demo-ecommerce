namespace Payment.Application.Services;

public enum PaymentWebhookProcessStatus
{
    Processed = 0,
    Duplicate = 1,
    InvalidSignature = 2,
    InvalidPayload = 3,
    SessionNotFound = 4
}

