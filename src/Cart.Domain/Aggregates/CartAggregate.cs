namespace Cart.Domain;

public sealed class CartAggregate
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Dictionary<Guid, CartLine> Lines { get; } = [];
    public decimal TotalAmount => Lines.Values.Sum(l => l.UnitPrice * l.Quantity);

    public void Apply(CartCreated @event)
    {
        Id = @event.CartId;
        UserId = @event.UserId;
    }

    public void Apply(CartItemAdded @event)
    {
        if (Lines.TryGetValue(@event.ProductId, out var existing))
        {
            existing.Quantity += @event.Quantity;
            return;
        }

        Lines[@event.ProductId] = new CartLine
        {
            ProductId = @event.ProductId,
            Sku = @event.Sku,
            Name = @event.Name,
            UnitPrice = @event.UnitPrice,
            Quantity = @event.Quantity
        };
    }

    public void Apply(CartItemRemoved @event)
    {
        Lines.Remove(@event.ProductId);
    }
}
