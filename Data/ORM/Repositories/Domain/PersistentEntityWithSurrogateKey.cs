using System;
using Composable.System;

namespace Composable.Data.ORM.Domain
{
    public class PersistentEntityWithSurrogateKey<TEntity,TKey> : PersistentEntity<TEntity> where TEntity : PersistentEntity<TEntity>
    {
        public virtual TKey PersistentId { get; private set; }
    }
}