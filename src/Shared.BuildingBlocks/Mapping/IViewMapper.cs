namespace Shared.BuildingBlocks.Mapping;

public interface IViewMapper<in TEntity, out TView>
{
    TView Map(TEntity entity);
}
