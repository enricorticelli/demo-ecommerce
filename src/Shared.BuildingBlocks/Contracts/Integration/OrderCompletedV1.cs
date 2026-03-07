namespace Shared.BuildingBlocks.Contracts.Integration;

public sealed record OrderCompletedV1(Guid OrderId, Guid CartId, Guid UserId, string TrackingCode, string TransactionId);
