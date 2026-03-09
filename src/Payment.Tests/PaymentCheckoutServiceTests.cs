using Moq;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Abstractions.Repositories;
using Payment.Application.Services;
using Xunit;

namespace Payment.Tests;

public sealed class PaymentCheckoutServiceTests
{
    [Fact]
    public async Task Ensure_checkout_should_call_provider_and_persist_redirect_when_pending()
    {
        var session = Domain.Entities.PaymentSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            150m,
            "paypal",
            string.Empty);

        var repository = new Mock<IPaymentSessionRepository>();
        repository
            .Setup(x => x.GetByOrderIdAsync(session.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var providerClient = new Mock<IPaymentProviderClient>();
        providerClient
            .Setup(x => x.CreateCheckoutAsync(It.IsAny<PaymentProviderCheckoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderCheckoutResult(
                "paypal",
                "chk_paypal_1",
                "http://localhost:8082/checkout/paypal/chk_paypal_1",
                "CREATED"));

        var router = new Mock<IPaymentProviderRouter>();
        router
            .Setup(x => x.ResolveByPaymentMethod("paypal"))
            .Returns(providerClient.Object);

        var sut = new PaymentCheckoutService(repository.Object, router.Object);

        var result = await sut.EnsureCheckoutByOrderIdAsync(session.OrderId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("paypal", result!.ProviderCode);
        Assert.Equal("chk_paypal_1", result.ExternalCheckoutId);
        Assert.Equal("http://localhost:8082/checkout/paypal/chk_paypal_1", result.RedirectUrl);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

