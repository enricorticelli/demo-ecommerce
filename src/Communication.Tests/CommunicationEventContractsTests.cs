using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Xunit;

namespace Communication.Tests;

public sealed class CommunicationEventContractsTests
{
    [Fact]
    public void Integration_events_should_be_versioned_with_V1_suffix()
    {
        var communicationEventTypes = typeof(IntegrationEventMetadata).Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass
                && !type.IsAbstract
                && type.Namespace is not null
                && (type.Namespace.Contains("IntegrationEvents.Order", StringComparison.Ordinal)
                    || type.Namespace.Contains("IntegrationEvents.Shipping", StringComparison.Ordinal))
                && typeof(IIntegrationEvent).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(communicationEventTypes);
        Assert.All(communicationEventTypes, type => Assert.EndsWith("V1", type.Name, StringComparison.Ordinal));
    }
}
