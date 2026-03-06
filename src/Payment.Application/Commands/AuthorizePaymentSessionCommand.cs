using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed record AuthorizePaymentSessionCommand(Guid SessionId) : ICommand<bool>;
