namespace Composable.Persistence
{
    ///<summary>Provides the ability to save and update persintent entities.</summary>
    public interface IEntityPersister
    {
        ///<summary>Fetches an entity and, if supported by implementation, eagerly locks it to avoid deadlock situations.</summary>
        TEntity GetForUpdate<TEntity>(object id);

        ///<summary>Fetches an entity if it exists and, if supported by implementation, eagerly locks it to avoid deadlock situations.</summary>
        bool TryGetForUpdate<TEntity>(object id, out TEntity model);

        ///<summary>Saves an entity, that was not previously persisted, to persistent storage </summary>
        void Save<TEntity>(TEntity entity);

        ///<summary>Deletes the entity with the supplied Id</summary>
        void Delete<TEntity>(object id);
    }
}