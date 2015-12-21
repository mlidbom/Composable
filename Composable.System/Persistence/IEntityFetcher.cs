namespace Composable.Persistence
{
    ///<summary>Provides the ability to get an entity by Id.</summary>
    public interface IEntityFetcher
    {
        ///<summary>Returns the entity with the given id.</summary>
        TEntity Get<TEntity>(object id);
        ///<summary>Returns true if the entity is found, false if not.</summary>
        bool TryGet<TEntity>(object id, out TEntity entity);
    }
}