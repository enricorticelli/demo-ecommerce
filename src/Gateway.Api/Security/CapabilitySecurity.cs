namespace Gateway.Api.Security;

public static class CapabilitySecurity
{
    public const string ClaimType = "capability";

    public const string CatalogProductsRead = "catalog.products.read";
    public const string CatalogProductsWrite = "catalog.products.write";
    public const string CatalogBrandsRead = "catalog.brands.read";
    public const string CatalogBrandsWrite = "catalog.brands.write";
    public const string CatalogCategoriesRead = "catalog.categories.read";
    public const string CatalogCategoriesWrite = "catalog.categories.write";
    public const string CatalogCollectionsRead = "catalog.collections.read";
    public const string CatalogCollectionsWrite = "catalog.collections.write";
    public const string WarehouseStockRead = "warehouse.stock.read";
    public const string WarehouseStockWrite = "warehouse.stock.write";

    public static string PolicyName(string capability) => $"Capability:{capability}";

    private static Func<IReadOnlySet<string>, bool> CreateRequirement(string requiredCapability)
    {
        var required = requiredCapability.Trim();
        var requiredPrefix = GetNamespacePrefix(required);

        return grantedCapabilities =>
        {
            if (grantedCapabilities.Contains("*") || grantedCapabilities.Contains(required))
            {
                return true;
            }

            return requiredPrefix is not null && grantedCapabilities.Contains($"{requiredPrefix}.*");
        };
    }

    public static bool IsSatisfiedBy(IEnumerable<string> grantedCapabilities, string requiredCapability)
    {
        var normalizedCapabilities = grantedCapabilities
            .Where(static capability => !string.IsNullOrWhiteSpace(capability))
            .Select(static capability => capability.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return CreateRequirement(requiredCapability)(normalizedCapabilities);
    }

    private static string? GetNamespacePrefix(string capability)
    {
        var separatorIndex = capability.IndexOf('.', StringComparison.Ordinal);
        return separatorIndex <= 0 ? null : capability[..separatorIndex];
    }
}