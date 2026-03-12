namespace Shared.BuildingBlocks.Exceptions;

public sealed class ForbiddenAppException(string message) : AppException(message);
