using System;
using System.Collections.Generic;
using Composable.GenericAbstractions.Time;

namespace Composable.Persistence.EventStore
{

    public interface IEventStored
    {
        Guid Id { get; }
        int Version { get; }
        IEnumerable<IAggregateEvent> GetChanges();
        void AcceptChanges();
        void LoadFromHistory(IEnumerable<IAggregateEvent> history);
        void SetTimeSource(IUtcTimeTimeSource timeSource);
        IObservable<IAggregateEvent> EventStream { get; }
    }

    public interface IEventStored<TEvent> : IEventStored where TEvent : IAggregateEvent
    {

    }
}