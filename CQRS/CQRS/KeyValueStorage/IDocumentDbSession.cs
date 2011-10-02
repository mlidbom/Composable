using System;
using System.Collections;
using System.Collections.Generic;
using Composable.DDD;
using Composable.UnitsOfWork;

namespace Composable.KeyValueStorage
{
    public interface IDocumentDbSession : IDisposable
    {
        TValue Get<TValue>(object key);
        bool TryGet<TValue>(object key, out TValue value);
        void Save<TValue>(object id, TValue value);
        void Delete<TEntity>(object id);

        void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>;
        void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>;        

        void SaveChanges();
        IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>;       
    }
}