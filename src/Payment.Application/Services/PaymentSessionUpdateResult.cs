using Payment.Application.Views;

namespace Payment.Application.Services;

public sealed record PaymentSessionUpdateResult(PaymentSessionView Session, bool StatusChanged);
