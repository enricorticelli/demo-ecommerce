using Shared.BuildingBlocks.Exceptions;

namespace Payment.Domain.Entities;

public sealed class PaymentSession
{
    public Guid SessionId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string PaymentMethod { get; private set; } = "stripe_card";
    public string Status { get; private set; } = "Pending";
    public string? TransactionId { get; private set; }
    public string? FailureReason { get; private set; }
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
