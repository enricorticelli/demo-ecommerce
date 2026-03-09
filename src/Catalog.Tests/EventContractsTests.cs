using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Xunit;

namespace Catalog.Tests;

public sealed class EventContractsTests
{
    [Fact]
    public void IntegrationEvents_WhenEnumerated_HaveV1Suffix()
    {
        var eventTypes = typeof(IntegrationEventMetadata).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("V1", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(eventTypes);
        Assert.All(eventTypes, type => Assert.EndsWith("V1", type.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void IntegrationEventMetadata_WhenCreated_HasRequiredFields()
    {
        var metadata = new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-123", "Catalog");

        Assert.NotEqual(Guid.Empty, metadata.EventId);
        Assert.False(string.IsNullOrWhiteSpace(metadata.CorrelationId));
        Assert.False(string.IsNullOrWhiteSpace(metadata.SourceContext));
    }
}

