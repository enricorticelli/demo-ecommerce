using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed record AuthorizePaymentCommand(PaymentAuthorizeRequestedV1 Request) : ICommand<PaymentAuthorizationResult>;
