using Shared.BuildingBlocks.Exceptions;

namespace Cart.Domain.Entities;

public sealed class Cart
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public List<CartItem> Items { get; private set; } = [];
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Cart()
    {
    }

    private Cart(Guid id, Guid userId)
    {
        Id = id;
        UserId = userId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public static Cart Create(Guid id, Guid userId)
    {
        if (id == Guid.Empty)
        {
            throw new ValidationAppException("Cart id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new ValidationAppException("User id is required.");
        }

        return new Cart(id, userId);
    }

    public void AddItem(CartItem item)
    {
        var existing = Items.FirstOrDefault(x => x.ProductId == item.ProductId);
        if (existing is null)
        {
            Items.Add(item);
        }
        else
        {
            existing.IncreaseQuantity(item.Quantity);
        }

        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public bool RemoveItem(Guid productId)
    {
        var existing = Items.FirstOrDefault(x => x.ProductId == productId);
        if (existing is null)
        {
            return false;
        }

        Items.Remove(existing);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return true;
    }

    public bool UpdateItemSku(Guid productId, string sku)
    {
        var existing = Items.FirstOrDefault(x => x.ProductId == productId);
        if (existing is null)
        {
            return false;
        }

        var changed = existing.UpdateSku(sku);
        if (changed)
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        return changed;
    }

    public decimal TotalAmount()
    {
        return Items.Sum(x => x.Quantity * x.UnitPrice);
    }

    public void Clear()
    {
        Items.Clear();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
