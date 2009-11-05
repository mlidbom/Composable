using System;

namespace Void.Data.ORM.Domain
{
    public class EntityWithSurrogateKey<TEntity,TKey> : Entity<TEntity> where TEntity : Entity<TEntity>
    {
        public virtual TKey PersistentId { get; private set; }
    }
}