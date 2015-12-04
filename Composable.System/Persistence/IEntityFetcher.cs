namespace Composable.Persistence
{
    public interface IEntityFetcher
    {
        TEntity Get<TEntity>(object id);
        bool TryGet<TEntity>(object id, out TEntity entity);
    }
}