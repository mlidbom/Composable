using System;
using System.Collections.Generic;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;

namespace Composable.Persistence.EventStore
{
    public interface IEventStored
    {
        Guid Id { get; }
        int Version { get; }
        IEnumerable<IDomainEvent> GetChanges();
        void AcceptChanges();
        void LoadFromHistory(IEnumerable<IDomainEvent> history);
        void SetTimeSource(IUtcTimeTimeSource timeSource);
        IObservable<IDomainEvent> EventStream { get; }
    }
}