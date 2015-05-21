using System;
using System.Collections.Generic;
using Composable.DDD;
using Composable.System.Linq;
using System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using log4net;
using Composable.System;

namespace Composable.KeyValueStorage
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public partial class DocumentDbSession : IDocumentDbSession, IUnitOfWorkParticipant
    {
        [ThreadStatic]
        internal static bool UseUpdateLock;

        private readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();

        public readonly IDocumentDb BackingStore;
        public readonly IDocumentDbSessionInterceptor Interceptor;        
        public readonly ISingleContextUseGuard UsageGuard;

        private readonly IDictionary<DocumentKey, DocumentItem> _handledDocuments = new Dictionary<DocumentKey, DocumentItem>(); 

        private static readonly ILog Log = LogManager.GetLogger(typeof(DocumentDbSession));

        //Review:mlidbo: Always requiring an interceptor causes a lot of unneeded complexity for clients. Consider creating a virtual void OnFirstLoad(T document) method instead. This would allow for inheriting this class to create "interceptable" sessions. Alternatively maybe an observable/event could be used somehow.
        public DocumentDbSession(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
        {
            UsageGuard = usageGuard;
            BackingStore = backingStore;
            Interceptor = interceptor;
        }

        public IObservable<IDocumentUpdated> DocumentUpdated { get { return BackingStore.DocumentUpdated; } }

        public virtual bool TryGet<TValue>(object key, out TValue document)
        {
            return TryGetInternal(key, typeof(TValue), out document);
        }

        private bool TryGetInternal<TValue>(object key, Type documentType, out TValue value)
        {
            if(documentType.IsInterface)
            {
                throw new ArgumentException("You cannot query by id for an interface type. There is no guarantee of uniqueness");
            }
            UsageGuard.AssertNoContextChangeOccurred(this);

            if (_idMap.TryGet(key, out value) && documentType.IsAssignableFrom(value.GetType()))
            {
                return true;
            }

            var documentItem = GetDocumentItem(key, documentType);
            if(!documentItem.IsDeleted && BackingStore.TryGet(key, out value, _persistentValues) && documentType.IsAssignableFrom(value.GetType()))
            {
                OnInitialLoad(key, value);
                return true;
            }

            return false;
        }

        private DocumentItem GetDocumentItem(object key, Type documentType)
        {
            DocumentItem doc;
            var documentKey = new DocumentKey(key, documentType);

            if (!_handledDocuments.TryGetValue(documentKey, out doc))
            {                
                doc = new DocumentItem(documentKey, BackingStore, this._persistentValues);
                _handledDocuments.Add(documentKey, doc);
            }
            return doc;
        }

        private void OnInitialLoad(object key, object value)
        {
            _idMap.Add(key, value);
            GetDocumentItem(key, value.GetType()).DocumentLoadedFromBackingStore(value);
            if (Interceptor != null)
                Interceptor.AfterLoad(value);
        }

        public virtual TValue GetForUpdate<TValue>(object key)
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            using(new UpdateLock())
            {
                return Get<TValue>(key);
            }
        }

        public virtual bool TryGetForUpdate<TValue>(object key, out TValue value)
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
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


        public IEnumerable<TValue> Get<TValue>(IEnumerable<Guid> ids) where TValue : IHasPersistentIdentity<Guid>
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            var idSet = ids.ToSet();//Avoid multiple enumerations.            

            var stored = BackingStore.GetAll<TValue>(idSet);

            stored.Where(document => !_idMap.Contains(typeof(TValue), document.Id))
                .ForEach(unloadedDocument => OnInitialLoad(unloadedDocument.Id, unloadedDocument));

            var results = _idMap.Select(pair => pair.Value).OfType<TValue>().Where(candidate => idSet.Contains(candidate.Id)).ToArray();
            var missingDocuments = idSet.Where(id => !results.Any(result => result.Id == id)).ToArray();
            if (missingDocuments.Any())
            {
                throw new NoSuchDocumentException(missingDocuments.First(), typeof(TValue));
            }
            return results;
        }

        public virtual TValue Get<TValue>(object key)
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            TValue value;
            if(TryGet(key, out value))
            {
                return value;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual void Save<TValue>(object id, TValue value)
        {
            UsageGuard.AssertNoContextChangeOccurred(this);            

            TValue ignored;
            if (TryGetInternal(id, value.GetType(), out ignored))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }

            var documentItem = GetDocumentItem(id, value.GetType());
            documentItem.Save(value);

            _idMap.Add(id, value);
            if(_unitOfWork == null)
            {                
                documentItem.CommitChangesToBackingStore();
            }else
            {
                Log.DebugFormat("{0} postponed persisting object from call to Save since participating in a unit of work", _id);
            }            
        }

        public virtual void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            if(entity.Id.Equals(Guid.Empty))
            {
                throw new DocumentIdIsEmptyGuidException();
            }
            Save(entity.Id, entity);
        }

        public virtual void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            Delete<TEntity>(entity.Id);
        }

        public virtual void Delete<T>(object id)
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            T ignored;
            if(!TryGet(id, out ignored))
            {
                throw new NoSuchDocumentException(id, typeof(T));
            }

            var documentItem = GetDocumentItem(id, typeof(T));
            documentItem.Delete();

            _idMap.Remove<T>(id);
            if(_unitOfWork == null)
            {
                documentItem.CommitChangesToBackingStore();
            }
            else
            {
                Log.DebugFormat("{0} postponed deleting object since participating in a unit of work", _id);
            }            
        }

        public virtual void SaveChanges()
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
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
            UsageGuard.AssertNoContextChangeOccurred(this);
            var stored = BackingStore.GetAll<T>();
            stored.Where(document => !_idMap.Contains(typeof (T), document.Id))
                .ForEach(unloadedDocument => OnInitialLoad(unloadedDocument.Id, unloadedDocument));
            return _idMap.Select(pair => pair.Value).OfType<T>();
        }

        public IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>
        {
            return BackingStore.GetAllIds<T>();
        }


        public virtual void Dispose()
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            //Can be called before the transaction commits....
            //_idMap.Clear();
        }

        public override string ToString()
        {
            return "{0}: {1}".FormatWith(_id, GetType().FullName);
        }

        private IUnitOfWork _unitOfWork;
        private readonly Guid _id = Guid.NewGuid();
        private Dictionary<Type, Dictionary<string, string>> _persistentValues = new Dictionary<Type, Dictionary<string, string>>();


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