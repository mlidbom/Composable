using System;
using System.Collections.Generic;
using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public class InMemoryKeyValueStoreSession : IKeyValueSession
    {
        private readonly InMemoryKeyValueStore _store;
        private Dictionary<Guid, object> _idMap = new Dictionary<Guid, object>();

        public InMemoryKeyValueStoreSession(InMemoryKeyValueStore store)
        {
            _store = store;
        }

        public TValue Load<TValue>(Guid key)
        {
            object value;
            if(_idMap.TryGetValue(key, out value))
            {
                return (TValue)value;
            }

            if(_store._store.TryGetValue(key, out value))
            {
                _idMap.Add(key, value);
                return (TValue)value;
            }

            throw new NoSuchKeyException(key);
        }

        public void Save<TValue>(Guid key, TValue value)
        {
            if(_idMap.ContainsKey(key) || _store._store.ContainsKey(key))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(key, value);
            }
            _idMap.Add(key, value);
        }

        public void SaveChanges()
        {
            _idMap.ForEach(entry => _store._store[entry.Key] = entry.Value);            
        }

        public void Dispose()
        {
            _idMap.Clear();
        }
    }
}