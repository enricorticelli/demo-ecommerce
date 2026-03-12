namespace Shared.BuildingBlocks.Api;

public static class AuthorizationClaimTypes
{
    public const string Realm = "realm";
    public const string Permission = "permission";
}

public static class AuthorizationPermissions
{
    public const string CatalogRead = "catalog:read";
    public const string CatalogWrite = "catalog:write";
    public const string OrdersRead = "orders:read";
    public const string OrdersWrite = "orders:write";
    public const string ShippingRead = "shipping:read";
    public const string ShippingWrite = "shipping:write";
    public const string WarehouseRead = "warehouse:read";
    public const string WarehouseWrite = "warehouse:write";
    public const string AccountRead = "account:read";
    public const string AccountWrite = "account:write";

    public static readonly string[] AllAdmin =
    [
        CatalogRead,
        CatalogWrite,
        OrdersRead,
        OrdersWrite,
        ShippingRead,
        ShippingWrite,
        WarehouseRead,
        WarehouseWrite,
        AccountRead,
        AccountWrite
    ];

    public static readonly string[] AllCustomer =
    [
        AccountRead,
        AccountWrite,
        OrdersRead
    ];
}

public static class AuthorizationPolicies
{
    public const string CustomerPolicy = "CustomerPolicy";
    public const string AdminPolicy = "AdminPolicy";

    public const string CatalogReadPolicy = "CatalogReadPolicy";
    public const string CatalogWritePolicy = "CatalogWritePolicy";
    public const string OrdersReadPolicy = "OrdersReadPolicy";
    public const string OrdersWritePolicy = "OrdersWritePolicy";
    public const string ShippingReadPolicy = "ShippingReadPolicy";
    public const string ShippingWritePolicy = "ShippingWritePolicy";
    public const string WarehouseReadPolicy = "WarehouseReadPolicy";
    public const string WarehouseWritePolicy = "WarehouseWritePolicy";
    public const string AccountAdminReadPolicy = "AccountAdminReadPolicy";
    public const string AccountAdminWritePolicy = "AccountAdminWritePolicy";
}