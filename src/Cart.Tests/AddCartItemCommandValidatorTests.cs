using Cart.Application;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Cart.Tests;

public sealed class AddCartItemCommandValidationTests
{
    [Fact]
    public void Should_fail_when_quantity_is_zero()
    {
        var command = new AddCartItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SKU-1",
            "Product",
            0,
            10m);

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            command,
            new ValidationContext(command),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, x => x.MemberNames.Contains(nameof(AddCartItemCommand.Quantity)));
    }

    [Fact]
    public void Moq_should_verify_test_probe_invocation()
    {
        var probe = new Mock<ITestProbe>();
        probe.Object.Ping("cart-tests");

        probe.Verify(x => x.Ping("cart-tests"), Times.Once);
    }
}
