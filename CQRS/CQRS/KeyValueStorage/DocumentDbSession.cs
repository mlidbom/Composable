using System;
using System.Collections.Generic;
using Composable.DDD;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Linq;
using System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using log4net;
using Composable.System;

namespace Composable.KeyValueStorage
{
    public partial class DocumentDbSession : IDocumentDbSession, IUnitOfWorkParticipant
    {
        [ThreadStatic]
        internal static bool UseUpdateLock;

// ReSharper disable InconsistentNaming
        internal readonly IObjectStore _backingStore;
        internal readonly IDocumentDbSessionInterceptor _interceptor;
// ReSharper restore InconsistentNaming

        private readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();
        private readonly ISingleContextUseGuard _usageGuard;

        private readonly IDictionary<DocumentKey, DocumentItem> _handledDocuments = new Dictionary<DocumentKey, DocumentItem>(); 

        private static readonly ILog Log = LogManager.GetLogger(typeof(DocumentDbSession));

        public DocumentDbSession(IDocumentDb store, ISingleContextUseGuard usageGuard, DocumentDbConfig config = null)
        {
            _usageGuard = usageGuard;
            if(config == null)
            {
                config = DocumentDbConfig.Default;
            }
            _backingStore = store.CreateStore();
            _interceptor = config.Interceptor;
        }


        public virtual bool TryGet<TValue>(object key, out TValue value)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);

            if (_idMap.TryGet(key, out value))
            {
                return true;
            }

            var documentItem = GetDocumentItem<TValue>(key);
            if(!documentItem.IsDeleted && _backingStore.TryGet(key, out value))
            {
                OnInitialLoad(key, value);
                return true;
            }

            return false;
        }

        private DocumentItem GetDocumentItem<TValue>(object key)
        {
            DocumentItem doc;
            var documentKey = new DocumentKey<TValue>(key);

            if (!_handledDocuments.TryGetValue(documentKey, out doc))
            {                
                doc = new DocumentItem(documentKey, _backingStore);
                _handledDocuments.Add(documentKey, doc);
            }
            return doc;
        }

        private void OnInitialLoad<TValue>(object key, TValue value)
        {
            _idMap.Add(key, value);
            GetDocumentItem<TValue>(key).DocumentLoadedFromBackingStore(value);
            if (_interceptor != null)
                _interceptor.AfterLoad(value);
        }

        public virtual TValue GetForUpdate<TValue>(object key)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            using(new UpdateLock())
            {
                return Get<TValue>(key);
            }
        }

        public virtual bool TryGetForUpdate<TValue>(object key, out TValue value)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            using (new UpdateLock())
            {
                return TryGet(key, out value);
            }
        }

        private class UpdateLock : IDisposable
        {
            public UpdateLock()
            {
                UseUpdateLock = true;
            }

            public void Dispose()
            {
                UseUpdateLock = false;
            }
        }

        public virtual TValue Get<TValue>(object key)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            TValue value;
            if(TryGet(key, out value))
            {
                return value;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual void Save<TValue>(object id, TValue value)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            TValue ignored;
            if (TryGet(id, out ignored))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }

            var documentItem = GetDocumentItem<TValue>(id);
            documentItem.Save(value);

            if(_unitOfWork == null)
            {                
                documentItem.CommitChangesToBackingStore();
            }else
            {
                Log.DebugFormat("{0} postponed persisting object from call to Save since participating in a unit of work", _id);
            }
            _idMap.Add(id, value);
        }

        public virtual void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            Save(entity.Id, entity);
        }

        public virtual void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            Delete<TEntity>(entity.Id);
        }

        public virtual void Delete<T>(object id)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            T ignored;
            if(!TryGet(id, out ignored))
            {
                throw new NoSuchDocumentException(id, typeof(T));
            }

            var documentItem = GetDocumentItem<T>(id);
            documentItem.Delete();

            if(_unitOfWork == null)
            {
                documentItem.CommitChangesToBackingStore();
            }
            else
            {
                Log.DebugFormat("{0} postponed deleting object since participating in a unit of work", _id);
            }
            _idMap.Remove<T>(id);
        }

        public virtual void SaveChanges()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
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
            _handledDocuments.ForEach(p => p.Value.CommitChangesToBackingStore());
        }

        public virtual IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            var stored = _backingStore.GetAll<T>();
            stored.Where(pair => !_idMap.Contains(typeof (T), pair.Key))
                .ForEach(pair => OnInitialLoad(pair.Key, pair.Value));

            return _idMap.Select(pair => pair.Value).OfType<T>();
        }



        public virtual void Dispose()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            //Can be called before the transaction commits....
            //_idMap.Clear();
        }

        public override string ToString()
        {
            return "{0}: {1}".FormatWith(_id, GetType().FullName);
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