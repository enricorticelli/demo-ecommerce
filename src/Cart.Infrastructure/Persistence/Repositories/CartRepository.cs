using Cart.Application.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Persistence.Repositories;

public sealed class CartRepository(CartDbContext dbContext) : ICartRepository
{
    public Task<Cart.Domain.Entities.Cart?> GetByIdAsync(Guid cartId, CancellationToken cancellationToken)
    {
        return dbContext.Carts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == cartId, cancellationToken);
    }

    public async Task<IReadOnlyList<Cart.Domain.Entities.Cart>> ListByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await dbContext.Carts
            .Include(x => x.Items)
            .Where(x => x.Items.Any(i => i.ProductId == productId))
            .ToArrayAsync(cancellationToken);
    }

    public void Add(Cart.Domain.Entities.Cart cart)
    {
        dbContext.Carts.Add(cart);
    }

    public void Remove(Cart.Domain.Entities.Cart cart)
    {
        dbContext.Carts.Remove(cart);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
