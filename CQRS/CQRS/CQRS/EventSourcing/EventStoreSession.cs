#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.ServiceBus;
using Composable.System.Linq;
using Composable.System;
using log4net;

#endregion

namespace Composable.CQRS.EventSourcing
{
    public abstract class EventStoreSession : IEventStoreSession
    {
        private readonly IServiceBus _bus;
        private static ILog Log = LogManager.GetLogger(typeof(EventStoreSession));
        protected readonly IDictionary<Guid, IEventStored> _idMap = new Dictionary<Guid, IEventStored>();

        protected abstract IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid aggregateId);

        protected EventStoreSession(IServiceBus bus)
        {
            _bus = bus;
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
            var aggregate = Activator.CreateInstance<TAggregate>();
            aggregate.LoadFromHistory(GetHistory(aggregateId).Where(e => e.AggregateRootVersion <= version));
            return aggregate;
        }

        public void Save<TAggregate>(TAggregate aggregate) where TAggregate : IEventStored
        {
            var changes = aggregate.GetChanges();
            if(aggregate.Version > 0 && changes.None() || changes.Any() && changes.Min(e => e.AggregateRootVersion) > 1)
            {
                throw new AttemptToSaveAlreadyPersistedAggregateException(aggregate);
            }
            if(aggregate.Version == 0 && changes.None())
            {
                throw new AttemptToSaveEmptyAggregate(aggregate);
            }
            _idMap.Add(aggregate.Id, aggregate);
        }

        public void SaveChanges()
        {
            Log.DebugFormat("saving changes with {0} changes from transaction", _idMap.Count);
            var newEvents = _idMap.SelectMany(p => p.Value.GetChanges());
            SaveEvents(newEvents);
            newEvents.ForEach(_bus.Publish);
            _idMap.Select(p => p.Value).ForEach(p => p.AcceptChanges());
        }

        protected abstract void SaveEvents(IEnumerable<IAggregateRootEvent> events);
        public abstract void Dispose();

        private readonly Guid Me = Guid.NewGuid();
      
    }

    public class AttemptToSaveEmptyAggregate : Exception
    {
        public AttemptToSaveEmptyAggregate(object value):base("Attempting to save an: {0} that Version=0 and no history to persist.".FormatWith(value.GetType().FullName))
        {
        }
    }
}