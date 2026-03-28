using Payment.Api.Contracts.Responses;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Xunit;

namespace Payment.Tests;

public sealed class SensitiveContractsTests
{
    [Fact]
    public void Contracts_WhenInspected_DoNotExposeCardFields()
    {
        var forbiddenTokens = new[]
        {
            "cardnumber",
            "pan",
            "cvv",
            "cvc",
            "expiry",
            "expmonth",
            "expyear",
            "iban"
        };

        var contractTypes = new[]
        {
            typeof(PaymentSessionResponse),
            typeof(PaymentAuthorizedV1),
            typeof(PaymentRejectedV1)
        };

        foreach (var type in contractTypes)
        {
            var propertyNames = type.GetProperties().Select(x => x.Name.ToLowerInvariant()).ToArray();

            Assert.DoesNotContain(propertyNames, property => forbiddenTokens.Any(property.Contains));
        }
    }
}

