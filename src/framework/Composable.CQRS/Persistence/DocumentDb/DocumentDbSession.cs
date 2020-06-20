using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Transactions;
using Composable.Contracts;
using Composable.DDD;
using Composable.Logging;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.Persistence.DocumentDb
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    partial class DocumentDbSession : IDocumentDbSession, IEnlistmentNotification
    {
        [ThreadStatic]
        internal static bool UseUpdateLock;

        readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();

        readonly IDocumentDb _backingStore;
        readonly ISingleContextUseGuard _usageGuard;

        readonly IDictionary<DocumentKey, DocumentItem> _handledDocuments = new Dictionary<DocumentKey, DocumentItem>();

        static readonly ILogger Log = Logger.For<DocumentDbSession>();

        public DocumentDbSession(IDocumentDb backingStore)
        {
            _usageGuard = new CombinationUsageGuard(new SingleThreadUseGuard(), new SingleTransactionUsageGuard());
            _backingStore = backingStore;
        }

        public virtual bool TryGet<TValue>(object key, [NotNullWhen(true)][MaybeNull]out TValue document) => TryGetInternal(key, typeof(TValue), out document);

        bool TryGetInternal<TValue>(object key, Type documentType, [NotNullWhen(true)][MaybeNull]out TValue value)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            EnsureParticipatingInTransaction();
            if (documentType.IsInterface)
            {
                throw new ArgumentException("You cannot query by id for an interface type. There is no guarantee of uniqueness");
            }

            if (_idMap.TryGet(key, out value) && documentType.IsInstanceOfType(value))
            {
                return true;
            }

            var documentItem = GetDocumentItem(key, documentType);
            if(!documentItem.IsDeleted && _backingStore.TryGet(key, out value, _persistentValues) && documentType.IsInstanceOfType(value))
            {
                OnInitialLoad(key, value!);
                return true;
            }

            return false;
        }

        DocumentItem GetDocumentItem(object key, Type documentType)
        {
            var documentKey = new DocumentKey(key, documentType);

            if (!_handledDocuments.TryGetValue(documentKey, out var doc))
            {
                doc = new DocumentItem(documentKey, _backingStore, _persistentValues);
                _handledDocuments.Add(documentKey, doc);
            }
            return doc;
        }

        void OnInitialLoad(object key, object value)
        {
            _idMap.Add(key, value);
            GetDocumentItem(key, value.GetType()).DocumentLoadedFromBackingStore(value);
        }

        public virtual TValue GetForUpdate<TValue>(object key)
        {
            using (new UpdateLock())
            {
                return Get<TValue>(key);
            }
        }

        class UpdateLock : IDisposable
        {
            public UpdateLock() => UseUpdateLock = true;

            public void Dispose()
            {
                UseUpdateLock = false;
            }
        }


        public IEnumerable<TValue> Get<TValue>(IEnumerable<Guid> ids) where TValue : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            var idSet = ids.ToSet();//Avoid multiple enumerations.

            var stored = _backingStore.GetAll<TValue>(idSet);

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
            _usageGuard.AssertNoContextChangeOccurred(this);
            EnsureParticipatingInTransaction();
#pragma warning disable 8600 //Review OK-ish: This is a Resharper bug. No real warning here.
            if (TryGet(key, out TValue value))
#pragma warning restore 8600
            {
                return value!;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual void Save<TValue>(object id, TValue value)
        {
            Contract.ArgumentNotNull(value, nameof(value));
            _usageGuard.AssertNoContextChangeOccurred(this);
            EnsureParticipatingInTransaction();

            if (TryGetInternal(id, value.GetType(), out TValue _))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }

            var documentItem = GetDocumentItem(id, value.GetType());
            documentItem.Save(value);

            _idMap.Add(id, value);
            documentItem.CommitChangesToBackingStore();
        }

        public virtual void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            EnsureParticipatingInTransaction();

            if (entity.Id.Equals(Guid.Empty))
            {
                throw new DocumentIdIsEmptyGuidException();
            }
            Save(entity.Id, entity);
        }

        public virtual void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            EnsureParticipatingInTransaction();

            Delete<TEntity>(entity.Id);
        }

        public virtual void Delete<T>(object id)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            EnsureParticipatingInTransaction();

            if(!TryGet(id, out T _))
            {
                throw new NoSuchDocumentException(id, typeof(T));
            }

            var documentItem = GetDocumentItem(id, typeof(T));
            documentItem.Delete();

            _idMap.Remove<T>(id);
            documentItem.CommitChangesToBackingStore();
        }

        public virtual IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            var stored = _backingStore.GetAll<T>();
            stored.Where(document => !_idMap.Contains(typeof (T), document.Id))
                .ForEach(unloadedDocument => OnInitialLoad(unloadedDocument.Id, unloadedDocument));
            return _idMap.Select(pair => pair.Value).OfType<T>();
        }

        public IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid> => _backingStore.GetAllIds<T>();

        public virtual void Dispose()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            //Can be called before the transaction commits....
            //_idMap.Clear();
        }

        public override string ToString() => $"{_id}: {GetType().FullName}";

        readonly Guid _id = Guid.NewGuid();
        readonly Dictionary<Type, Dictionary<string, string>> _persistentValues = new Dictionary<Type, Dictionary<string, string>>();


#pragma warning disable IDE0069 //Reviewed OK: We should really not dispose the transaction just because we reference it :)
        Transaction? _participatingIn = null;
#pragma warning restore IDE0069 // Disposable fields should be disposed
        void EnsureParticipatingInTransaction()
        {
            var ambientTransaction = Transaction.Current;
            if(ambientTransaction != null)
            {
                if(_participatingIn == null)
                {
                    _participatingIn = ambientTransaction;
                    ambientTransaction.EnlistVolatile(this, EnlistmentOptions.EnlistDuringPrepareRequired);
                }
                else if(_participatingIn != ambientTransaction)
                {
                    throw new Exception($"Somehow switched to a new transaction. Original: {_participatingIn.TransactionInformation.LocalIdentifier} new: {ambientTransaction.TransactionInformation.LocalIdentifier}");
                }
            }
        }

        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            using(var transactionScope = new TransactionScope(_participatingIn))
            {
                Log.Debug($"{_id} saving changes. Unit of work: {1}");
                _handledDocuments.ForEach(p => p.Value.CommitChangesToBackingStore());
                transactionScope.Complete();
            }
            preparingEnlistment.Prepared();
        }

        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            enlistment.Done();
            _participatingIn = null;
        }

        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            enlistment.Done();
        }

        void IEnlistmentNotification.InDoubt(Enlistment enlistment) => enlistment.Done();
    }
}