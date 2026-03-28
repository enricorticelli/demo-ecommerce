using Order.Application.Commands;

namespace Order.Application.Abstractions.Rules;

public interface IOrderRules
{
    void EnsureCreateCommandIsValid(CreateOrderCommand command);
    string NormalizeTrackingCode(string? trackingCode);
    string NormalizeTransactionId(string? transactionId);
    string NormalizeCancelReason(string? reason);
}
