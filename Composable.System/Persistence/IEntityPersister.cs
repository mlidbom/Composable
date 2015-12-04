namespace Composable.Persistence
{
    public interface IEntityPersister
    {
        TEntity GetForUpdate<TEntity>(object id);
        bool TryGetForUpdate<TEntity>(object id, out TEntity model);
        void Save<TEntity>(TEntity entity);
        void Delete<TEntity>(object id);
    }
}