using System.Linq;

namespace Composable.Persistence
{
    ///<summary>Allows for querying persistent entities using Linq</summary>
    public interface IQueryableEntities
    {
        ///<summary>
        /// <para>Returns an <see cref="IQueryable{TEntity}"/> that allows you to query the persistent entities.</para>
        /// <para>Supported operations vary with the implementations.</para>
        /// </summary>
        IQueryable<TEntity> Query<TEntity>();
    }
}