using System;
using System.Collections.Generic;
using Composable.DDD;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Linq;
using System.Linq;
using Composable.UnitsOfWork;
using log4net;

namespace Composable.KeyValueStorage
{
    public class DocumentDbSession : IDocumentDbSession, IUnitOfWorkParticipant
    {
        private readonly IObjectStore _backingStore;
        private readonly IDocumentDbSessionInterceptor _interceptor;

        private readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();
        private readonly InMemoryObjectStore _newlyAdded = new InMemoryObjectStore();

        private static ILog Log = LogManager.GetLogger(typeof(DocumentDbSession));

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
            if(_unitOfWork == null)
            {                
                _backingStore.Add(id, value); 
            }else
            {
                Log.DebugFormat("{0} postponed persisting object from call to Save since participating in a unit of work", _id);
                _newlyAdded.Add(id, value);
            }
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
            if (_unitOfWork == null)
            {                
                InternalSaveChanges();
            }else
            {
                Log.DebugFormat("{0} Ignored call to SaveChanges since participating in a unit of work", _id);
            }
        }

        private void InternalSaveChanges()
        {
            Log.DebugFormat("{0} saving changes. Unit of work: {1}",_id, _unitOfWork ?? (object)"null");
            _newlyAdded.ForEach(p => _backingStore.Add(p.Key, p.Value));
            _newlyAdded.Clear();
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

        private IUnitOfWork _unitOfWork;
        private readonly Guid _id = Guid.NewGuid();


        IUnitOfWork IUnitOfWorkParticipant.UnitOfWork { get { return _unitOfWork; } }
        Guid IUnitOfWorkParticipant.Id { get { return _id; } }

        void IUnitOfWorkParticipant.Join(IUnitOfWork unit)
        {
            _unitOfWork = unit;
        }

        void IUnitOfWorkParticipant.Commit(IUnitOfWork unit)
        {
            InternalSaveChanges();
            _unitOfWork = null;
        }

        void IUnitOfWorkParticipant.Rollback(IUnitOfWork unit)
        {
            _unitOfWork = null;
        }
    }
}