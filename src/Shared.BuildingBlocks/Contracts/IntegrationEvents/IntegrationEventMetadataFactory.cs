namespace Shared.BuildingBlocks.Contracts.IntegrationEvents;

public static class IntegrationEventMetadataFactory
{
    public static IntegrationEventMetadata Create(string correlationId, string sourceContext)
    {
        return new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, correlationId, sourceContext);
    }
}
