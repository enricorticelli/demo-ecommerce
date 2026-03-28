using Shared.BuildingBlocks.Exceptions;

namespace Order.Domain.ValueObjects;

public sealed class OrderItem
{
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private OrderItem()
    {
    }

    private OrderItem(Guid productId, string sku, string name, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Sku = sku;
        Name = name;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public static OrderItem Create(Guid productId, string sku, string name, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationAppException("Order item product id is required.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ValidationAppException("Order item SKU is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationAppException("Order item name is required.");
        }

        if (quantity <= 0)
        {
            throw new ValidationAppException("Order item quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new ValidationAppException("Order item unit price must be greater than zero.");
        }

        return new OrderItem(productId, sku.Trim(), name.Trim(), quantity, unitPrice);
    }
}
