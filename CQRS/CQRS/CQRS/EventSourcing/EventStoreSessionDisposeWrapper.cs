using System;
using System.Transactions;

namespace Composable.CQRS.EventSourcing
{
    public class EventStoreSessionDisposeWrapper : IEventStoreSession
    {
        private readonly EventStoreSession _session;

        public EventStoreSessionDisposeWrapper(EventStoreSession session)
        {
            _session = session;
        }

        public void Dispose()
        {
            _session.DisposeIfNotEnlisted();
        }

        public TAggregate Get<TAggregate>(Guid aggregateId) where TAggregate : IEventStored
        {
            return _session.Get<TAggregate>(aggregateId);
        }

        public TAggregate LoadSpecificVersion<TAggregate>(Guid aggregateId, int version) where TAggregate : IEventStored
        {
            return _session.LoadSpecificVersion<TAggregate>(aggregateId, version);
        }

        public void Save<TAggregate>(TAggregate aggregate) where TAggregate : IEventStored
        {
            _session.Save(aggregate);
        }

        public void SaveChanges()
        {
            _session.SaveChanges();
        }
    }
}