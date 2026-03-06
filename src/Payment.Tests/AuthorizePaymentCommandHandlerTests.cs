using Moq;
using Payment.Application;
using Shared.BuildingBlocks.Contracts;
using Xunit;

namespace Payment.Tests;

public sealed class AuthorizePaymentCommandHandlerTests
{
    [Fact]
    public async Task Authorize_command_should_delegate_to_payment_service()
    {
        var request = new PaymentAuthorizeRequestedV1(Guid.NewGuid(), Guid.NewGuid(), 120m);
        var expected = new PaymentAuthorizationResult(request.OrderId, true, "TX-1");

        var service = new Mock<IPaymentService>();
        service.Setup(x => x.AuthorizeAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var sut = new AuthorizePaymentCommandHandler(service.Object);
        var actual = await sut.HandleAsync(new AuthorizePaymentCommand(request), CancellationToken.None);

        Assert.Equal(expected, actual);
    }
}
