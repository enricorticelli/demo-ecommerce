using Cart.Api.Application;
using FluentAssertions;

namespace Cart.UnitTests;

public sealed class AddCartItemCommandValidatorTests
{
    [Fact]
    public void Should_fail_when_quantity_is_zero()
    {
        var validator = new AddCartItemCommandValidator();
        var command = new AddCartItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SKU-1",
            "Product",
            0,
            10m);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(AddCartItemCommand.Quantity));
    }
}
