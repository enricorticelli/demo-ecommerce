using Order.Api.Contracts.Requests;
using Order.Api.Mappers;
using Xunit;

namespace Order.Tests;

public sealed class OrderMapperSecurityTests
{
    [Fact]
    public void ToCreateCommand_UsesAuthenticatedUserIdFromJwtAndIgnoresPayloadIdentityFields()
    {
        var jwtUserId = Guid.NewGuid();

        var request = new CreateOrderRequest(
            CartId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            IdentityType: "anonymous",
            PaymentMethod: "card",
            Items: [new OrderItemRequest(Guid.NewGuid(), "SKU-1", "Item 1", 1, 10m)],
            TotalAmount: 10m,
            AuthenticatedUserId: Guid.NewGuid(),
            AnonymousId: Guid.NewGuid(),
            Customer: new OrderCustomerRequest("Mario", "Rossi", "mario@example.com", "+390000"),
            ShippingAddress: new OrderAddressRequest("Street", "Rome", "00100", "IT"),
            BillingAddress: new OrderAddressRequest("Street", "Rome", "00100", "IT"));

        var command = request.ToCreateCommand(jwtUserId, "corr-123");

        Assert.Equal(jwtUserId, command.UserId);
        Assert.Equal("Authenticated", command.IdentityType);
        Assert.Equal(jwtUserId, command.AuthenticatedUserId);
        Assert.Null(command.AnonymousId);
    }

    [Fact]
    public void ToCreateCommand_WithoutJwt_UsesAnonymousIdentity()
    {
        var anonymousId = Guid.NewGuid();

        var request = new CreateOrderRequest(
            CartId: Guid.NewGuid(),
            UserId: anonymousId,
            IdentityType: "authenticated",
            PaymentMethod: "card",
            Items: [new OrderItemRequest(Guid.NewGuid(), "SKU-1", "Item 1", 1, 10m)],
            TotalAmount: 10m,
            AuthenticatedUserId: Guid.NewGuid(),
            AnonymousId: anonymousId,
            Customer: new OrderCustomerRequest("Mario", "Rossi", "mario@example.com", "+390000"),
            ShippingAddress: new OrderAddressRequest("Street", "Rome", "00100", "IT"),
            BillingAddress: new OrderAddressRequest("Street", "Rome", "00100", "IT"));

        var command = request.ToCreateCommand(null, "corr-123");

        Assert.Equal(anonymousId, command.UserId);
        Assert.Equal("Anonymous", command.IdentityType);
        Assert.Null(command.AuthenticatedUserId);
        Assert.Equal(anonymousId, command.AnonymousId);
    }
}
