using System;
using System.Collections.Generic;
using Composable.GenericAbstractions.Time;

namespace Composable.Persistence.EventStore
{

    public interface IEventStored
    {
        Guid Id { get; }
        int Version { get; }
        IReadOnlyList<IAggregateEvent> GetChanges();
        void AcceptChanges();
        void LoadFromHistory(IEnumerable<IAggregateEvent> history);
        void SetTimeSource(IUtcTimeTimeSource timeSource);
        IObservable<IAggregateEvent> EventStream { get; }
    }

    // ReSharper disable once UnusedTypeParameter it is used for type information in various parts of the framework.
    public interface IEventStored<TEvent> : IEventStored where TEvent : IAggregateEvent
    {

    }
}