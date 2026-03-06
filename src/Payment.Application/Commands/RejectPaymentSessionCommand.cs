using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed record RejectPaymentSessionCommand(Guid SessionId, string Reason) : ICommand<bool>;
