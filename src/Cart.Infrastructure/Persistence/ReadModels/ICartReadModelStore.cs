namespace Cart.Infrastructure.Persistence.ReadModels;

public interface ICartReadModelStore : Shared.BuildingBlocks.ReadModels.IReadModelStore<Guid, CartReadModelRow>
{
}
