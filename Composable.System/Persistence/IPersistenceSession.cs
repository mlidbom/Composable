using System;

namespace Composable.Persistence
{
    public interface IPersistenceSession : IDisposable, IQueryableEntities, IQueryableEntityFetcher, IEntityFetcher, IEntityPersister
    {        
    }
}
