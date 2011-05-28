using System;
using System.Collections;
using System.Collections.Generic;
using Composable.DDD;

namespace Composable.KeyValueStorage
{
    public interface IKeyValueSession : IDisposable
    {
        TValue Get<TValue>(Guid key);
        bool TryGet<TValue>(Guid key, out TValue value);
        void Save<TValue>(Guid key, TValue value);
        void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>;
        void SaveChanges();
        IEnumerable<T> GetAll<T>();
    }
}