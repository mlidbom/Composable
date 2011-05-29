using System;
using System.Collections.Generic;
using Composable.DDD;

namespace Composable.KeyValueStorage
{
    public class KeyValueStoreSessionProxy : IKeyValueStoreSession
    {
            private readonly IKeyValueStoreSession _session;

            protected KeyValueStoreSessionProxy(IKeyValueStoreSession session)
            {
                _session = session;
            }

            public TValue Get<TValue>(Guid key)
            {
                return _session.Get<TValue>(key);
            }

            public bool TryGet<TValue>(Guid key, out TValue value)
            {
                return _session.TryGet(key, out value);
            }

            public void Save<TValue>(Guid key, TValue value)
            {
                _session.Save(key, value);
            }

            public void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
            {
                _session.Save(entity);
            }

            public void SaveChanges()
            {
                _session.SaveChanges();
            }

            public IEnumerable<T> GetAll<T>()
            {
                return _session.GetAll<T>();
            }

            public void Dispose()
            {
                _session.Dispose();
            }
        }    
}