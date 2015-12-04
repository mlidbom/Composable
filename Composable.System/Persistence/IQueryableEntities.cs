using System.Linq;

namespace Composable.Persistence
{
    public interface IQueryableEntities
    {
        IQueryable<TEntity> Query<TEntity>();
    }
}