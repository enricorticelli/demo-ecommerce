namespace Shared.BuildingBlocks.Api.Pagination;

public static class PaginationNormalizer
{
    public static (int Limit, int Offset) Normalize(
        int? limit,
        int? offset,
        int defaultLimit = 50,
        int maxLimit = 200)
    {
        var normalizedLimit = Math.Clamp(limit ?? defaultLimit, 1, maxLimit);
        var normalizedOffset = Math.Max(offset ?? 0, 0);
        return (normalizedLimit, normalizedOffset);
    }
}
