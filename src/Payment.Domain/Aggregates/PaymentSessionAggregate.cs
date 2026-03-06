namespace Payment.Domain;

public sealed class PaymentSessionAggregate
{
    public Guid SessionId { get; }
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public decimal Amount { get; }
    public PaymentSessionStatus Status { get; private set; }
    public string? TransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public PaymentSessionAggregate(Guid sessionId, Guid orderId, Guid userId, decimal amount, DateTimeOffset createdAtUtc)
    {
        SessionId = sessionId;
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        CreatedAtUtc = createdAtUtc;
        Status = PaymentSessionStatus.Pending;
    }

    public bool Authorize(string transactionId, DateTimeOffset completedAtUtc)
    {
        if (Status != PaymentSessionStatus.Pending)
        {
            return false;
        }

        Status = PaymentSessionStatus.Authorized;
        TransactionId = transactionId;
        CompletedAtUtc = completedAtUtc;
        return true;
    }

    public bool Reject(string reason, DateTimeOffset completedAtUtc)
    {
        if (Status != PaymentSessionStatus.Pending)
        {
            return false;
        }

        Status = PaymentSessionStatus.Rejected;
        FailureReason = reason;
        CompletedAtUtc = completedAtUtc;
        return true;
    }
}
