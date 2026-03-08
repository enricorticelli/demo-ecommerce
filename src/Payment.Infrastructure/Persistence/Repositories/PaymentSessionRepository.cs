using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions.Repositories;
using PaymentSessionEntity = Payment.Domain.Entities.PaymentSession;

namespace Payment.Infrastructure.Persistence.Repositories;

public sealed class PaymentSessionRepository(PaymentDbContext dbContext) : IPaymentSessionRepository
{
    public Task<PaymentSessionEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return dbContext.PaymentSessions
            .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
    }

    public Task<PaymentSessionEntity?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return dbContext.PaymentSessions
            .FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentSessionEntity>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.PaymentSessions
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public void Add(PaymentSessionEntity session)
    {
        dbContext.PaymentSessions.Add(session);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
