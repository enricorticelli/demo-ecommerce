using Moq;
using Payment.Application.Abstractions.Commands;
using Payment.Application.Abstractions.Idempotency;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Abstractions.Repositories;
using Payment.Application.Services;
using Xunit;

namespace Payment.Tests;

public sealed class PaymentWebhookServiceTests
{
    [Fact]
    public async Task ProcessAsync_InvalidSignature_ReturnsInvalidSignatureStatus()
    {
        var sessionRepository = new Mock<IPaymentSessionRepository>();
        var dedupStore = new Mock<IPaymentWebhookDeduplicationStore>();
        var router = new Mock<IPaymentProviderRouter>();
        var commandService = new Mock<IPaymentCommandService>();

        router
            .Setup(x => x.ResolveWebhookVerifier("stripe"))
            .Returns(new StubVerifier("stripe", false));

        var sut = new PaymentWebhookService(
            sessionRepository.Object,
            dedupStore.Object,
            router.Object,
            commandService.Object);

        var result = await sut.ProcessAsync(
            "stripe",
            "{}",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            "corr-1",
            CancellationToken.None);

        Assert.Equal(PaymentWebhookProcessStatus.InvalidSignature, result.Status);
    }

    [Fact]
    public async Task ProcessAsync_AuthorizedWebhook_AuthorizesSessionAndMarksProcessed()
    {
        var session = Domain.Entities.PaymentSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            120m,
            "stripe_card",
            string.Empty);

        var sessionRepository = new Mock<IPaymentSessionRepository>();
        sessionRepository
            .Setup(x => x.GetBySessionIdAsync(session.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sessionRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dedupStore = new Mock<IPaymentWebhookDeduplicationStore>();
        dedupStore
            .Setup(x => x.HasProcessedAsync("stripe", "evt_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var router = new Mock<IPaymentProviderRouter>();
        router
            .Setup(x => x.ResolveWebhookVerifier("stripe"))
            .Returns(new StubVerifier("stripe", true));
        router
            .Setup(x => x.ResolveByProviderCode("stripe"))
            .Returns(new StubProviderClient(
                "stripe",
                new PaymentWebhookNotification(
                    "evt_001",
                    session.SessionId,
                    "chk_001",
                    "checkout.session.completed",
                    PaymentWebhookDecision.Authorized,
                    "TX-1",
                    null)));

        var commandService = new Mock<IPaymentCommandService>();
        commandService
            .Setup(x => x.AuthorizeAsync(session.SessionId, "corr-2", "TX-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentSessionUpdateResult?)null);

        var sut = new PaymentWebhookService(
            sessionRepository.Object,
            dedupStore.Object,
            router.Object,
            commandService.Object);

        var result = await sut.ProcessAsync(
            "stripe",
            "{\"eventId\":\"evt_001\"}",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            "corr-2",
            CancellationToken.None);

        Assert.Equal(PaymentWebhookProcessStatus.Processed, result.Status);
        commandService.Verify(x => x.AuthorizeAsync(session.SessionId, "corr-2", "TX-1", It.IsAny<CancellationToken>()), Times.Once);
        dedupStore.Verify(x => x.MarkProcessedAsync("stripe", "evt_001", It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class StubVerifier(string providerCode, bool valid) : IPaymentWebhookVerifier
    {
        public string ProviderCode => providerCode;
        public bool VerifySignature(string rawPayload, IReadOnlyDictionary<string, string> headers) => valid;
    }

    private sealed class StubProviderClient(string providerCode, PaymentWebhookNotification notification) : IPaymentProviderClient
    {
        public string ProviderCode => providerCode;
        public bool SupportsPaymentMethod(string paymentMethod) => false;
        public Task<PaymentProviderCheckoutResult> CreateCheckoutAsync(PaymentProviderCheckoutRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public PaymentWebhookNotification ParseWebhook(string rawPayload) => notification;
    }
}

