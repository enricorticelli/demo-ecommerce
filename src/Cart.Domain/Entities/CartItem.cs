using Shared.BuildingBlocks.Exceptions;

namespace Cart.Domain.Entities;

public sealed class CartItem
{
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private CartItem()
    {
    }

    private CartItem(Guid productId, string sku, string name, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Sku = sku;
        Name = name;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public static CartItem Create(Guid productId, string sku, string name, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationAppException("Product id is required.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ValidationAppException("Sku is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationAppException("Name is required.");
        }

        if (quantity <= 0)
        {
            throw new ValidationAppException("Quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new ValidationAppException("Unit price must be greater than zero.");
        }

        return new CartItem(productId, sku.Trim(), name.Trim(), quantity, unitPrice);
    }

    public void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ValidationAppException("Quantity must be greater than zero.");
        }

        Quantity += quantity;
    }

    public bool UpdateSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ValidationAppException("Sku is required.");
        }

        var normalized = sku.Trim();
        if (string.Equals(Sku, normalized, StringComparison.Ordinal))
        {
            return false;
        }

        Sku = normalized;
        return true;
    }
}
