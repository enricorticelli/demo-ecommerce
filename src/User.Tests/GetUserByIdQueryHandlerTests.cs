using Moq;
using User.Application;
using Xunit;

namespace User.Tests;

public sealed class GetUserByIdQueryHandlerTests
{
    [Fact]
    public async Task Get_user_query_should_delegate_to_user_service()
    {
        var userId = Guid.NewGuid();
        var expected = new UserView(userId, "user@example.com", "User Test");

        var service = new Mock<IUserService>();
        service.Setup(x => x.GetUserByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var sut = new GetUserByIdQueryHandler(service.Object);
        var actual = await sut.HandleAsync(new GetUserByIdQuery(userId), CancellationToken.None);

        Assert.Equal(expected, actual);
    }
}
