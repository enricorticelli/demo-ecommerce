using Shared.BuildingBlocks.Exceptions;

namespace Payment.Domain.Entities;

public sealed class PaymentSession
{
    public Guid SessionId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string PaymentMethod { get; private set; } = "stripe_card";
    public string ProviderCode { get; private set; } = string.Empty;
    public string ExternalCheckoutId { get; private set; } = string.Empty;
    public string ProviderStatus { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending";
    public string? TransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public string LastWebhookEventId { get; private set; } = string.Empty;
    public string LastProviderPayload { get; private set; } = string.Empty;
    public DateTimeOffset? LastWebhookReceivedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string RedirectUrl { get; private set; } = string.Empty;

    private PaymentSession()
    {
    }

    public static PaymentSession Create(Guid orderId, Guid userId, decimal amount, string paymentMethod, string redirectUrl)
    {
        if (orderId == Guid.Empty)
        {
            throw new ValidationAppException("Order id is required.");
        }

        if (amount < 0)
        {
            throw new ValidationAppException("Payment amount cannot be negative.");
        }

        return new PaymentSession
        {
            SessionId = Guid.NewGuid(),
            OrderId = orderId,
            UserId = userId,
            Amount = amount,
            PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "stripe_card" : paymentMethod.Trim(),
            Status = "Pending",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RedirectUrl = redirectUrl
        };
    }

    public static PaymentSession Create(Guid orderId, string redirectUrl)
    {
        return Create(orderId, Guid.Empty, 0m, "stripe_card", redirectUrl);
    }

    public bool UpdateCheckoutContext(Guid userId, decimal amount, string paymentMethod)
    {
        if (amount < 0)
        {
            throw new ValidationAppException("Payment amount cannot be negative.");
        }

        var normalizedMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "stripe_card" : paymentMethod.Trim();
        var changed = false;

        if (UserId != userId)
        {
            UserId = userId;
            changed = true;
        }

        if (Amount != amount)
        {
            Amount = amount;
            changed = true;
        }

        if (!string.Equals(PaymentMethod, normalizedMethod, StringComparison.Ordinal))
        {
            PaymentMethod = normalizedMethod;
            ProviderCode = string.Empty;
            ExternalCheckoutId = string.Empty;
            ProviderStatus = string.Empty;
            RedirectUrl = string.Empty;
            changed = true;
        }

        return changed;
    }

    public bool ConfigureProviderCheckout(
        string providerCode,
        string externalCheckoutId,
        string redirectUrl,
        string providerStatus)
    {
        var normalizedProviderCode = string.IsNullOrWhiteSpace(providerCode)
            ? string.Empty
            : providerCode.Trim().ToLowerInvariant();
        var normalizedExternalCheckoutId = string.IsNullOrWhiteSpace(externalCheckoutId)
            ? string.Empty
            : externalCheckoutId.Trim();
        var normalizedProviderStatus = string.IsNullOrWhiteSpace(providerStatus)
            ? string.Empty
            : providerStatus.Trim();

        var changed = false;

        if (!string.Equals(ProviderCode, normalizedProviderCode, StringComparison.Ordinal))
        {
            ProviderCode = normalizedProviderCode;
            changed = true;
        }

        if (!string.Equals(ExternalCheckoutId, normalizedExternalCheckoutId, StringComparison.Ordinal))
        {
            ExternalCheckoutId = normalizedExternalCheckoutId;
            changed = true;
        }

        if (!string.Equals(ProviderStatus, normalizedProviderStatus, StringComparison.Ordinal))
        {
            ProviderStatus = normalizedProviderStatus;
            changed = true;
        }

        if (!string.Equals(RedirectUrl, redirectUrl, StringComparison.Ordinal))
        {
            RedirectUrl = redirectUrl;
            changed = true;
        }

        return changed;
    }

    public bool UpdateRedirectUrl(string redirectUrl)
    {
        if (string.Equals(RedirectUrl, redirectUrl, StringComparison.Ordinal))
        {
            return false;
        }

        RedirectUrl = redirectUrl;
        return true;
    }

    public bool RegisterProviderWebhook(
        string providerCode,
        string? externalCheckoutId,
        string providerStatus,
        string externalEventId,
        string rawPayload)
    {
        var changed = false;

        var normalizedProviderCode = string.IsNullOrWhiteSpace(providerCode)
            ? string.Empty
            : providerCode.Trim().ToLowerInvariant();
        if (!string.Equals(ProviderCode, normalizedProviderCode, StringComparison.Ordinal))
        {
            ProviderCode = normalizedProviderCode;
            changed = true;
        }

        var normalizedExternalCheckoutId = string.IsNullOrWhiteSpace(externalCheckoutId)
            ? string.Empty
            : externalCheckoutId.Trim();
        if (!string.Equals(ExternalCheckoutId, normalizedExternalCheckoutId, StringComparison.Ordinal))
        {
            ExternalCheckoutId = normalizedExternalCheckoutId;
            changed = true;
        }

        var normalizedProviderStatus = string.IsNullOrWhiteSpace(providerStatus)
            ? string.Empty
            : providerStatus.Trim();
        if (!string.Equals(ProviderStatus, normalizedProviderStatus, StringComparison.Ordinal))
        {
            ProviderStatus = normalizedProviderStatus;
            changed = true;
        }

        var normalizedExternalEventId = string.IsNullOrWhiteSpace(externalEventId)
            ? string.Empty
            : externalEventId.Trim();
        if (!string.Equals(LastWebhookEventId, normalizedExternalEventId, StringComparison.Ordinal))
        {
            LastWebhookEventId = normalizedExternalEventId;
            changed = true;
        }

        var sanitizedPayload = string.IsNullOrWhiteSpace(rawPayload) ? string.Empty : rawPayload.Trim();
        if (sanitizedPayload.Length > 4096)
        {
            sanitizedPayload = sanitizedPayload[..4096];
        }

        if (!string.Equals(LastProviderPayload, sanitizedPayload, StringComparison.Ordinal))
        {
            LastProviderPayload = sanitizedPayload;
            changed = true;
        }

        var now = DateTimeOffset.UtcNow;
        if (!LastWebhookReceivedAtUtc.HasValue || LastWebhookReceivedAtUtc.Value != now)
        {
            changed = true;
        }
        LastWebhookReceivedAtUtc = now;
        return changed;
    }

    public bool Authorize(string transactionId)
    {
        if (string.Equals(Status, "Authorized", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        Status = "Authorized";
        TransactionId = transactionId;
        FailureReason = null;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        return true;
    }

    public bool Reject(string reason)
    {
        if (string.Equals(Status, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        Status = "Rejected";
        FailureReason = reason;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        return true;
    }
}
