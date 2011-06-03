using System;
using System.Collections.Generic;

namespace Composable.KeyValueStorage
{
    public interface IObjectStore
    {
        bool TryGet<T>(Guid id, out T value);
        void Add<T>(Guid id, T value);
        bool Remove<T>(Guid id);
        void Update(Guid key, object value);
        IEnumerable<KeyValuePair<Guid, T>> GetAll<T>();
    }
}