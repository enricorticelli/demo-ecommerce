using System.Collections.Concurrent;
using Payment.Application;
using Payment.Domain;
using Shared.BuildingBlocks.Contracts;
using Wolverine;

namespace Payment.Infrastructure;

public sealed class PaymentService(IMessageBus bus) : IPaymentService, IPaymentSessionService
{
    private const decimal MaxAcceptedAmount = 10000m;
    private static readonly ConcurrentDictionary<Guid, PaymentSessionAggregate> SessionsById = new();
    private static readonly ConcurrentDictionary<Guid, Guid> SessionIdByOrderId = new();

    public async Task<PaymentAuthorizationResult> AuthorizeAsync(PaymentAuthorizeRequestedV1 request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0 || request.Amount > MaxAcceptedAmount)
        {
            await bus.PublishAsync(new PaymentFailedV1(request.OrderId, "Payment declined"));
            return new PaymentAuthorizationResult(request.OrderId, false);
        }

        var mode = Environment.GetEnvironmentVariable("PAYMENT_PROVIDER_MODE") ?? "redirect";
        if (mode.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            var transactionId = $"TX-{Guid.NewGuid():N}";
            await bus.PublishAsync(new PaymentAuthorizedV1(request.OrderId, transactionId));
            return new PaymentAuthorizationResult(request.OrderId, true, transactionId);
        }

        var existing = await GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existing is not null)
        {
            return new PaymentAuthorizationResult(request.OrderId, false, PaymentSessionId: existing.SessionId);
        }

        var sessionId = Guid.NewGuid();
        var aggregate = new PaymentSessionAggregate(sessionId, request.OrderId, request.UserId, request.Amount, DateTimeOffset.UtcNow);

        SessionsById[sessionId] = aggregate;
        SessionIdByOrderId[request.OrderId] = sessionId;

        return new PaymentAuthorizationResult(request.OrderId, false, PaymentSessionId: sessionId);
    }

    public Task<PaymentSessionView?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!SessionIdByOrderId.TryGetValue(orderId, out var sessionId))
        {
            return Task.FromResult<PaymentSessionView?>(null);
        }

        return GetBySessionIdAsync(sessionId, cancellationToken);
    }

    public Task<PaymentSessionView?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!SessionsById.TryGetValue(sessionId, out var aggregate))
        {
            return Task.FromResult<PaymentSessionView?>(null);
        }

        return Task.FromResult<PaymentSessionView?>(MapToView(aggregate));
    }

    public async Task<bool> AuthorizeSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!SessionsById.TryGetValue(sessionId, out var aggregate))
        {
            return false;
        }

        var transactionId = $"TX-{Guid.NewGuid():N}";
        if (!aggregate.Authorize(transactionId, DateTimeOffset.UtcNow))
        {
            return false;
        }

        await bus.PublishAsync(new PaymentAuthorizedV1(aggregate.OrderId, transactionId));
        return true;
    }

    public async Task<bool> RejectSessionAsync(Guid sessionId, string reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!SessionsById.TryGetValue(sessionId, out var aggregate))
        {
            return false;
        }

        var finalReason = string.IsNullOrWhiteSpace(reason) ? "Payment declined" : reason;
        if (!aggregate.Reject(finalReason, DateTimeOffset.UtcNow))
        {
            return false;
        }

        await bus.PublishAsync(new PaymentFailedV1(aggregate.OrderId, finalReason));
        return true;
    }

    private static PaymentSessionView MapToView(PaymentSessionAggregate aggregate)
    {
        var frontendBase = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000";
        var redirectUrl = $"{frontendBase.TrimEnd('/')}/payment/session/{aggregate.SessionId}?orderId={aggregate.OrderId}";

        return new PaymentSessionView(
            aggregate.SessionId,
            aggregate.OrderId,
            aggregate.UserId,
            aggregate.Amount,
            aggregate.Status.ToString(),
            aggregate.TransactionId,
            aggregate.FailureReason,
            aggregate.CreatedAtUtc,
            aggregate.CompletedAtUtc,
            redirectUrl);
    }
}
