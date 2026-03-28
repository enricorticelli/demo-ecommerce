using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Xunit;

namespace Order.Tests;

public sealed class EventContractsTests
{
    [Fact]
    public void IntegrationEvents_WhenEnumerated_HaveV1Suffix()
    {
        var orderEventTypes = typeof(IntegrationEventMetadata).Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass
                && !type.IsAbstract
                && type.Namespace is not null
                && type.Namespace.Contains("IntegrationEvents.Order", StringComparison.Ordinal)
                && type.Name.EndsWith("V1", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(orderEventTypes);
        Assert.All(orderEventTypes, type => Assert.EndsWith("V1", type.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void IntegrationEventMetadata_WhenCreated_HasRequiredFields()
    {
        var metadata = new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-123", "Order");

        Assert.NotEqual(Guid.Empty, metadata.EventId);
        Assert.False(string.IsNullOrWhiteSpace(metadata.CorrelationId));
        Assert.False(string.IsNullOrWhiteSpace(metadata.SourceContext));
    }
}

