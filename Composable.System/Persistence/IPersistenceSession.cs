using System;

namespace Composable.Persistence
{
    ///<summary>
    /// <para>Wraps a number of other interfaces together for convenience. It is generally recommended to use the more fine grained interfaces instead.</para>
    /// <para>Consider using one of these instead: <see cref="IEntityFetcher"/>, <see cref="IEntityPersister"/>, <see cref="IQueryableEntities"/>, <see cref="IQueryableEntityFetcher"/></para>
    /// </summary>
    public interface IPersistenceSession : IDisposable, IQueryableEntities, IQueryableEntityFetcher, IEntityFetcher, IEntityPersister
    {        
    }
}
