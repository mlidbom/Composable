using System;
using System.Collections.Generic;
using Composable.DDD;
using Composable.System.Linq;
using System.Linq;

namespace Composable.KeyValueStorage
{
    public class KeyValueSession : IKeyValueStoreSession
    {
        private readonly IObjectStore _backingStore;

        private readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();


        public KeyValueSession(IObjectStore backingStore)
        {
            _backingStore = backingStore;
        }


        public bool TryGet<TValue>(Guid key, out TValue value)
        {
            if (_idMap.TryGet(key, out value))
            {
                return true;
            }

            if (_backingStore.TryGet(key, out value))
            {
                _idMap.Add(key, value);
                return true;
            }

            return false;
        }

        public TValue Get<TValue>(Guid key)
        {
            TValue value;
            if(TryGet(key, out value))
            {
                return value;
            }

            throw new NoSuchKeyException(key, typeof(TValue));
        }

        public void Save<TValue>(Guid id, TValue value)
        {
            if (_idMap.Contains<TValue>(id))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }
            _backingStore.Add(id, value);
            _idMap.Add(id, value);
        }

        public void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            Save(entity.Id, entity);
        }

        public void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            Delete<TEntity>(entity.Id);
        }

        public void Delete<T>(Guid id)
        {
            if (!_backingStore.Remove<T>(id))
            {
                throw new NoSuchKeyException(id, typeof(T));
            }
            _idMap.Remove<T>(id);
        }

        public void SaveChanges()
        {
            _backingStore.Update(_idMap.AsEnumerable());
        }

        public IEnumerable<T> GetAll<T>()
        {
            var stored = _backingStore.GetAll<T>();
            stored.Where(pair => !_idMap.Contains(typeof (T), pair.Key))
                .ForEach(pair => _idMap.Add(pair.Key, pair.Value));

            return _idMap.Select(pair => pair.Value).OfType<T>();
        }

        

        public void Dispose()
        {
            //Can be called before the transaction commits....
            //_idMap.Clear();
        }
    }
}