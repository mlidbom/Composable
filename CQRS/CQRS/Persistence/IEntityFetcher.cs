using System.Linq;

namespace Composable.Persistence
{
    public interface IEntityFetcher
    {
        TEntity Get<TEntity>(object entityId);        
        IQueryable<T> Query<T>();
    }
}