using System;
using System.Collections.Generic;
using Composable.DDD;

namespace Composable.KeyValueStorage
{
    public interface IDocumentDbSession : IDisposable, IDocumentUpdatedNotifier
    {
        TValue Get<TValue>(object key);
        bool TryGet<TValue>(object key, out TValue value);
        
        /// <summary>Like Get but, if supported by implementing class, eagerly locks the instance in the database.</summary>
        TValue GetForUpdate<TValue>(object key);
        /// <summary>Like Get TryGet but , if supported by implementing class, eagerly locks the instance in the database.</summary>
        bool TryGetForUpdate<TValue>(object key, out TValue value);
        void Save<TValue>(object id, TValue value);
        void Delete<TEntity>(object id);

        void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>;
        void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>;        

        void SaveChanges();
        IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>;
        IEnumerable<T> Get<T>(IEnumerable<Guid> ids ) where T : IHasPersistentIdentity<Guid>;       
    }
}