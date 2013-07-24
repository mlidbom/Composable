using System;
using System.Collections.Generic;
using Composable.DDD;

namespace Composable.KeyValueStorage
{
    public interface IObjectStore : IDisposable
    {
        bool TryGet<T>(object id, out T value);
        void Add<T>(object id, T value);
        bool Remove<T>(object id);
        bool Remove(object id, Type documentType);
        void Update(IEnumerable<KeyValuePair<string, object>> values);
        IEnumerable<KeyValuePair<Guid, T>> GetAll<T>() where T : IHasPersistentIdentity<Guid>;
    }
}