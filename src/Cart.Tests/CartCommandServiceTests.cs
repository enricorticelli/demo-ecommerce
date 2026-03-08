using Cart.Application.Abstractions.Repositories;
using Cart.Application.Commands;
using Cart.Application.Services;
using Cart.Application.Views;
using Moq;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;
using Xunit;

namespace Cart.Tests;

public sealed class CartCommandServiceTests
{
    [Fact]
    public async Task Add_item_should_create_new_cart_when_missing()
    {
        var repository = new Mock<ICartRepository>();
        repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Cart.Domain.Entities.Cart?)null);

        var mapper = new Mock<IViewMapper<Cart.Domain.Entities.Cart, CartView>>();
        mapper
            .Setup(x => x.Map(It.IsAny<Cart.Domain.Entities.Cart>()))
            .Returns((Cart.Domain.Entities.Cart cart) => new CartView(cart.Id, cart.UserId, [], cart.TotalAmount()));

        var sut = new CartCommandService(repository.Object, mapper.Object);

        await sut.AddItemAsync(
            new AddCartItemCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Item 1", 1, 10m),
            CancellationToken.None);

        repository.Verify(x => x.Add(It.IsAny<Cart.Domain.Entities.Cart>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remove_item_should_throw_when_cart_missing()
    {
        var repository = new Mock<ICartRepository>();
        repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Cart.Domain.Entities.Cart?)null);

        var mapper = new Mock<IViewMapper<Cart.Domain.Entities.Cart, CartView>>();
        var sut = new CartCommandService(repository.Object, mapper.Object);

        var action = async () => await sut.RemoveItemAsync(new RemoveCartItemCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(action);
    }

    [Fact]
    public async Task Checkout_should_clear_items_after_result()
    {
        var cart = Cart.Domain.Entities.Cart.Create(Guid.NewGuid(), Guid.NewGuid());
        cart.AddItem(Cart.Domain.Entities.CartItem.Create(Guid.NewGuid(), "SKU-1", "Item 1", 2, 10m));

        var repository = new Mock<ICartRepository>();
        repository.Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

        var mapper = new Mock<IViewMapper<Cart.Domain.Entities.Cart, CartView>>();
        var sut = new CartCommandService(repository.Object, mapper.Object);

        var result = await sut.CheckoutAsync(new CheckoutCartCommand(cart.Id), CancellationToken.None);

        Assert.Equal(20m, result.TotalAmount);
        Assert.Empty(cart.Items);
    }
}
