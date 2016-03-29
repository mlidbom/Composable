namespace Composable.Persistence
{
    ///<summary>Combines the services of <see cref="IQueryableEntities"/> and <see cref="IEntityFetcher"/> allowing you to fetch entities by id, or to query them using Linq</summary>
    public interface IQueryableEntityFetcher : IQueryableEntities, IEntityFetcher
    {        
    }
}