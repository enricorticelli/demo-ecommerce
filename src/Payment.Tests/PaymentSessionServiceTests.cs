using Moq;
using Payment.Application.Abstractions.Repositories;
using Payment.Application.Services;
using Xunit;

namespace Payment.Tests;

public sealed class PaymentSessionServiceTests
{
    [Fact]
    public async Task Get_by_order_should_return_null_when_missing()
    {
        var repository = new Mock<IPaymentSessionRepository>();
        repository
            .Setup(x => x.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.PaymentSession?)null);

        var sut = new PaymentSessionService(repository.Object);

        var result = await sut.GetByOrderIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Reject_should_redact_pan_like_values()
    {
        var session = Domain.Entities.PaymentSession.Create(Guid.NewGuid(), "http://localhost/payment/session/1");

        var repository = new Mock<IPaymentSessionRepository>();
        repository
            .Setup(x => x.GetBySessionIdAsync(session.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var sut = new PaymentSessionService(repository.Object);

        var result = await sut.RejectAsync(session.SessionId, "Card 4111111111111111 refused", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Rejected", result!.Session.Status);
        Assert.Contains("[REDACTED]", result.Session.FailureReason, StringComparison.Ordinal);
    }
}
