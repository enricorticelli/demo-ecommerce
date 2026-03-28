using Shared.BuildingBlocks.Exceptions;

namespace Warehouse.Domain.Entities;

public sealed class WarehouseStockItem
{
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public int AvailableQuantity { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private WarehouseStockItem()
    {
    }

    private WarehouseStockItem(Guid productId, string sku, int availableQuantity)
    {
        ProductId = productId;
        Sku = sku;
        AvailableQuantity = availableQuantity;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static WarehouseStockItem Create(Guid productId, string sku, int availableQuantity)
    {
        Validate(productId, sku, availableQuantity);
        return new WarehouseStockItem(productId, sku.Trim(), availableQuantity);
    }

    public void Upsert(string sku, int availableQuantity)
    {
        Validate(ProductId, sku, availableQuantity);
        Sku = sku.Trim();
        AvailableQuantity = availableQuantity;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public bool TryReserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ValidationAppException("Quantity must be greater than zero.");
        }

        if (AvailableQuantity < quantity)
        {
            return false;
        }

        AvailableQuantity -= quantity;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return true;
    }

    private static void Validate(Guid productId, string sku, int availableQuantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationAppException("Product id is required.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ValidationAppException("Sku is required.");
        }

        if (availableQuantity < 0)
        {
            throw new ValidationAppException("Available quantity cannot be negative.");
        }
    }
}
