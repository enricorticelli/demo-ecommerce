namespace Order.Infrastructure.Persistence.ReadModels;

public interface IOrderReadModelStore : Shared.BuildingBlocks.ReadModels.IReadModelStore<Guid, OrderReadModelRow>
{
}
