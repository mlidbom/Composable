using System.Linq;

namespace Composable.Persistence
{
    public interface IEntityReader
    {
        TEntity Get<TEntity>(object entityId);        
        IQueryable<T> Query<T>();
    }
}