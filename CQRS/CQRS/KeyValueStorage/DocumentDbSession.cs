using System;
using System.Collections.Generic;
using Composable.DDD;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Linq;
using System.Linq;

namespace Composable.KeyValueStorage
{
    public class DocumentDbSession : IDocumentDbSession
    {
        private readonly IObjectStore _backingStore;
        private readonly IDocumentDbSessionInterceptor _interceptor;

        private readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();


        public DocumentDbSession(IDocumentDb store, DocumentDbConfig config = null)
        {
            if(config == null)
            {
                config = DocumentDbConfig.Default;
            }
            _backingStore = store.CreateStore();
            _interceptor = config.Interceptor;
        }


        public bool TryGet<TValue>(object key, out TValue value)
        {
            if (_idMap.TryGet(key, out value))
            {
                return true;
            }

            if (_backingStore.TryGet(key, out value))
            {
                _idMap.Add(key, value);
                if (_interceptor != null)
                    _interceptor.AfterLoad(value);
                return true;
            }

            return false;
        }

        public TValue Get<TValue>(object key)
        {
            TValue value;
            if(TryGet(key, out value))
            {
                return value;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public void Save<TValue>(object id, TValue value)
        {
            if (_idMap.Contains(value.GetType(), id))
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

        public void Delete<T>(object id)
        {
            if (!_backingStore.Remove<T>(id))
            {
                throw new NoSuchDocumentException(id, typeof(T));
            }
            _idMap.Remove<T>(id);
        }

        public void SaveChanges()
        {
            _backingStore.Update(_idMap.AsEnumerable());
        }

        public IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>
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