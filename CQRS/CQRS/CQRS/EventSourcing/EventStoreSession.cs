#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.System.Linq;
using Composable.System;
using log4net;

#endregion

namespace Composable.CQRS.EventSourcing
{
    public abstract class EventStoreSession : IEventStoreSession, IEnlistmentNotification
    {
        private static ILog Log = LogManager.GetLogger(typeof(EventStoreSession));
        protected readonly IDictionary<Guid, IEventStored> _idMap = new Dictionary<Guid, IEventStored>();
        private bool _enlisted;
        private bool _disposedScheduledForAfterTransactionDone;

        protected abstract IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid aggregateId);

        protected EventStoreSession()
        {
            EnlistInAmbientTransaction();
        }

        private IEnumerable<IAggregateRootEvent> GetHistory(Guid aggregateId)
        {
            var history = GetHistoryUnSafe(aggregateId);
            if(history.None())
            {
                throw new Exception(string.Format("Aggregate root with Id: {0} not found", aggregateId));
            }

            int version = 1;
            foreach(var aggregateRootEvent in history)
            {
                if(aggregateRootEvent.AggregateRootVersion != version++)
                {
                    throw new InvalidHistoryException();
                }
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
            Log.DebugFormat("saving changes with {0} changes from transaction", _idMap.Count);
            SaveEvents(_idMap.SelectMany(p => p.Value.GetChanges()));
            _idMap.Select(p => p.Value).ForEach(p => p.AcceptChanges());
        }

        protected abstract void SaveEvents(IEnumerable<IAggregateRootEvent> events);
        public abstract void Dispose();

        private readonly Guid Me = Guid.NewGuid();
        private void EnlistInAmbientTransaction()
        {
            if(Transaction.Current != null && !_enlisted)
            {
                Transaction.Current.EnlistVolatile(this, EnlistmentOptions.EnlistDuringPrepareRequired);
                _enlisted = true;

                Log.DebugFormat("enlisting in local: {0} in {1}", Me, Transaction.Current.TransactionInformation.LocalIdentifier);
                Log.DebugFormat("enlisting in distributed: {0} in {1}", Me, Transaction.Current.TransactionInformation.DistributedIdentifier);
            }
        }

        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            try
            {
                Log.DebugFormat("prepare called with {0} changes from transaction", _idMap.Count);
                SaveChanges();
                preparingEnlistment.Prepared();
                DisposeIfScheduled();
                Log.DebugFormat("prepare completed with {0} changes from transaction", _idMap.Count);
            }catch(Exception e)
            {
                Log.Error("prepare failed", e);
                preparingEnlistment.ForceRollback(e);                
            }
        }

        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            try
            {
                Log.DebugFormat("commit called with {0} changes from transaction", _idMap.Count);
                _enlisted = false;                
                DisposeIfScheduled();
            }catch(Exception e)
            {
                Log.Error("Commit failed", e);
            }finally
            {
                enlistment.Done();
            }
        }        

        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            try
            {
                Log.DebugFormat("rollback called with {0} changes from transaction", _idMap.Count);
                _enlisted = false;
                DisposeIfScheduled();
            }catch(Exception e)
            {
                Log.Error("Rollback failed", e);
            }finally
            {
                enlistment.Done();
            }
        }

        public void InDoubt(Enlistment enlistment)
        {
            try
            {
                Log.DebugFormat("indoubt called with {0} changes from transaction", _idMap.Count);
                _enlisted = false;
                DisposeIfScheduled();
            }catch(Exception e)
            {
                Log.Error("Indoubt failed", e);
            }finally
            {
                enlistment.Done();
            }
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
    }
}