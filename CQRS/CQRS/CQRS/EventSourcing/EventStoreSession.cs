#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Composable.System.Linq;
using Composable.System;

#endregion

namespace Composable.CQRS.EventSourcing
{
    public abstract class EventStoreSession : IEventStoreSession, IEnlistmentNotification
    {
        protected readonly IDictionary<Guid, IEventStored> _idMap = new Dictionary<Guid, IEventStored>();
        private bool _enlisted;
        private bool _disposedScheduledForAfterTransactionDone;

        protected abstract IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid aggregateId);

        private IEnumerable<IAggregateRootEvent> GetHistory(Guid aggregateId)
        {
            var history = GetHistoryUnSafe(aggregateId);
            if(history.None())
            {
                throw new Exception(string.Format("Aggregate root with Id: {0} not found", aggregateId));
            }
            return history;
        }

        public TAggregate Get<TAggregate>(Guid aggregateId) where TAggregate : IEventStored
        {
            EnlistInAmbientTransaction();

            IEventStored existing;
            if(_idMap.TryGetValue(aggregateId, out existing))
            {
                return (TAggregate)existing;
            }

            var aggregate = Activator.CreateInstance<TAggregate>();
            aggregate.LoadFromHistory(GetHistory(aggregateId));
            _idMap.Add(aggregateId, aggregate);
            return aggregate;
        }

        public TAggregate LoadSpecificVersion<TAggregate>(Guid aggregateId, int version) where TAggregate : IEventStored
        {
            EnlistInAmbientTransaction();

            var aggregate = Activator.CreateInstance<TAggregate>();
            aggregate.LoadFromHistory(GetHistory(aggregateId).Where(e => e.AggregateRootVersion <= version));
            return aggregate;
        }

        public void Save<TAggregate>(TAggregate aggregate) where TAggregate : IEventStored
        {
            EnlistInAmbientTransaction();

            var changes = aggregate.GetChanges();
            if(aggregate.Version > 0 && changes.None() || changes.Any() && changes.Min(e => e.AggregateRootVersion) > 1)
            {
                throw new AttemptToSaveAlreadyPersistedAggregateException(aggregate);
            }
            _idMap.Add(aggregate.Id, aggregate);
        }

        public void SaveChanges()
        {
            Log("saving changes with {0} changes from transaction", _idMap.Count);
            SaveEvents(_idMap.SelectMany(p => p.Value.GetChanges()));
            _idMap.Select(p => p.Value).ForEach(p => p.AcceptChanges());
        }

        protected abstract void SaveEvents(IEnumerable<IAggregateRootEvent> events);
        public abstract void Dispose();

        private void EnlistInAmbientTransaction()
        {
            if(Transaction.Current != null && !_enlisted)
            {
                Transaction.Current.EnlistVolatile(this, EnlistmentOptions.EnlistDuringPrepareRequired);
                _enlisted = true;
                Log("enlisted in transaction {0}", Transaction.Current.TransactionInformation.LocalIdentifier);
            }
        }

        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            try
            {
                Log("prepare called with {0} changes from transaction", _idMap.Count);
                SaveChanges();
                preparingEnlistment.Prepared();
                Log("prepare completed with {0} changes from transaction", _idMap.Count);
            }catch(Exception e)
            {
                Log("prepare failed with: {0}", e);
            }
        }

        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            Log("commit called with {0} changes from transaction", _idMap.Count);
            _enlisted = false;
            enlistment.Done();
            DisposeIfScheduled();
        }        

        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            Log("rollback called with {0} changes from transaction", _idMap.Count);
            _enlisted = false;
            DisposeIfScheduled();
        }

        public void InDoubt(Enlistment enlistment)
        {
            Log("indoubt called with {0} changes from transaction", _idMap.Count);
            _enlisted = false;
            enlistment.Done();
        }

        private void DisposeIfScheduled()
        {
            if (_disposedScheduledForAfterTransactionDone)
            {
                Dispose();
            }
        }        

        public void DisposeIfNotEnlisted()
        {
            if(_enlisted)
            {
                _disposedScheduledForAfterTransactionDone = true;
            }else
            {
                Dispose();
            }
        }

        private void Log(string message, params object[] @params)
        {
            Console.WriteLine("{0} : ".FormatWith(GetType().Name)  + " " + message, @params);
        }
    }
}